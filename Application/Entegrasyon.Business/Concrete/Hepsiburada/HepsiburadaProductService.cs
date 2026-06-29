using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Diagnostics;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Hepsiburada;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Hepsiburada;

/// <summary>
/// Hepsiburada ürün publish servisi (gerçek API çağrıları).
/// Pipeline: Validation → Map → Multipart Upload → trackingId
/// </summary>
public sealed class HepsiburadaProductService(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IHepsiburadaApiClient apiClient,
    IHepsiburadaProductMapper productMapper,
    HepsiburadaMappingValidator mappingValidator,
    IProductActivityLogger activityLogger,
    ILogger<HepsiburadaProductService> logger) : IHepsiburadaProductService
{
    private const int HbMarketPlaceId = MarketPlaceConstants.HepsiburadaMarketPlaceId;

    public async Task<IDataResult<string>> PublishProductAsync(Guid productId)
    {
        var sw = Stopwatch.StartNew();
        using var activity = EntegrasyonActivitySource.StartProductSync("Hepsiburada", productId);
        try
        {
            // 1. Validation
            using (var validationSpan = EntegrasyonActivitySource.StartValidation("Hepsiburada"))
            {
                var validationResult = await mappingValidator.ValidateProductMappingsAsync(productId);
                validationSpan?.SetTag("validation.success", validationResult.Success);

                await activityLogger.LogAsync(productId, ProductActivityType.MappingValidated,
                    validationResult.Success ? "Hepsiburada mapping doğrulaması başarılı" : validationResult.Message!,
                    validationResult.Success ? ProductActivityStatus.Success : ProductActivityStatus.Error,
                    marketplaceName: "Hepsiburada");

                if (!validationResult.Success)
                {
                    validationSpan?.SetStatus(ActivityStatusCode.Error, validationResult.Message);
                    activity?.SetStatus(ActivityStatusCode.Error, "Validation failed");
                    EntegrasyonMetrics.ProductSyncErrors.Add(1,
                        new KeyValuePair<string, object?>("marketplace", "Hepsiburada"),
                        new KeyValuePair<string, object?>("error_type", "validation"));
                    return new ErrorDataResult<string>(null!, validationResult.Message!);
                }
            }

            // 2. Mapping
            string json;
            using (var mappingSpan = EntegrasyonActivitySource.StartMapping("Hepsiburada"))
            {
                var mapResult = await productMapper.MapProductAsync(productId);
                mappingSpan?.SetTag("mapping.success", mapResult.Success);

                if (!mapResult.Success)
                {
                    mappingSpan?.SetStatus(ActivityStatusCode.Error, mapResult.Message);
                    activity?.SetStatus(ActivityStatusCode.Error, "Mapping failed");
                    EntegrasyonMetrics.ProductSyncErrors.Add(1,
                        new KeyValuePair<string, object?>("marketplace", "Hepsiburada"),
                        new KeyValuePair<string, object?>("error_type", "mapping"));
                    return new ErrorDataResult<string>(null!, mapResult.Message!);
                }

                json = JsonSerializer.Serialize(mapResult.Data);
            }

            // 3. Publish (multipart JSON upload)
            var apiSw = Stopwatch.StartNew();
            var response = await apiClient.PostMultipartJsonFileAsync("/api/products/import", json, "products.json");
            apiSw.Stop();
            EntegrasyonMetrics.MarketplaceApiDuration.Record(apiSw.ElapsedMilliseconds,
                new KeyValuePair<string, object?>("marketplace", "Hepsiburada"),
                new KeyValuePair<string, object?>("operation", "publish"));

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                logger.LogError("HB publish failed: {Status} {Body}", response.StatusCode, errorBody);
                activity?.SetStatus(ActivityStatusCode.Error, $"HTTP {response.StatusCode}");
                await activityLogger.LogAsync(productId, ProductActivityType.BatchFailed,
                    $"Hepsiburada API hatası: {response.StatusCode}",
                    ProductActivityStatus.Error, errorBody, "Hepsiburada");
                EntegrasyonMetrics.ProductSyncErrors.Add(1,
                    new KeyValuePair<string, object?>("marketplace", "Hepsiburada"),
                    new KeyValuePair<string, object?>("error_type", "api_error"));
                return new ErrorDataResult<string>(null!, $"API hatası: {response.StatusCode}");
            }

            var trackingResponse = await response.Content
                .ReadFromJsonAsync<HepsiburadaTrackingResponse>();

            if (trackingResponse?.Success != true || trackingResponse.Data?.TrackingId == null)
            {
                var msg = trackingResponse?.Message ?? "trackingId alınamadı";
                activity?.SetStatus(ActivityStatusCode.Error, msg);
                return new ErrorDataResult<string>(null!, msg);
            }

            var trackingId = trackingResponse.Data.TrackingId;
            activity?.SetTag("hepsiburada.tracking_id", trackingId);

            // Update ProductMarketplace
            await using var dbContext = await contextFactory.CreateDbContextAsync();
            var pm = await dbContext.ProductMarketplaces
                .AsTracking()
                .FirstOrDefaultAsync(x => x.ProductId == productId && x.MarketPlaceId == HbMarketPlaceId);

            if (pm != null)
            {
                pm.BatchRequestId = trackingId;
                pm.UpdatedAt = DateTimeOffset.UtcNow;
                await dbContext.SaveChangesAsync();
            }

            await activityLogger.LogAsync(productId, ProductActivityType.PublishSent,
                "Hepsiburada'ya ürün gönderildi",
                ProductActivityStatus.Success, null, "Hepsiburada", trackingId);

            sw.Stop();
            EntegrasyonMetrics.ProductSyncTotal.Add(1,
                new KeyValuePair<string, object?>("marketplace", "Hepsiburada"));
            EntegrasyonMetrics.ProductSyncDuration.Record(sw.ElapsedMilliseconds,
                new KeyValuePair<string, object?>("marketplace", "Hepsiburada"));

            activity?.SetStatus(ActivityStatusCode.Ok);
            return new SuccessDataResult<string>(trackingId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HB publish exception for product {ProductId}", productId);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            await activityLogger.LogAsync(productId, ProductActivityType.BatchFailed,
                $"Hepsiburada publish hatası: {ex.Message}",
                ProductActivityStatus.Error, ex.ToString(), "Hepsiburada");
            EntegrasyonMetrics.ProductSyncErrors.Add(1,
                new KeyValuePair<string, object?>("marketplace", "Hepsiburada"),
                new KeyValuePair<string, object?>("error_type", "exception"));
            return new ErrorDataResult<string>(null!, $"Hata: {ex.Message}");
        }
    }

    public async Task<IDataResult<List<HepsiburadaProductStatusItem>>> CheckProductStatusAsync(string trackingId)
    {
        try
        {
            var response = await apiClient.GetAsync($"/api/products/status/{trackingId}");

            if (!response.IsSuccessStatusCode)
            {
                return new ErrorDataResult<List<HepsiburadaProductStatusItem>>(
                    null!, $"Status API hatası: {response.StatusCode}");
            }

            var statusResponse = await response.Content
                .ReadFromJsonAsync<HepsiburadaProductStatusResponse>();

            if (statusResponse?.Success != true || statusResponse.Data?.Content == null)
            {
                return new ErrorDataResult<List<HepsiburadaProductStatusItem>>(
                    null!, statusResponse?.Message ?? "Status bilgisi alınamadı");
            }

            return new SuccessDataResult<List<HepsiburadaProductStatusItem>>(statusResponse.Data.Content);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HB status check failed for trackingId {TrackingId}", trackingId);
            return new ErrorDataResult<List<HepsiburadaProductStatusItem>>(null!, $"Hata: {ex.Message}");
        }
    }

    public async Task<IResult> ApprovePreMatchAsync(string merchantSku)
    {
        try
        {
            await using var dbContext = await contextFactory.CreateDbContextAsync();
            var marketplace = await dbContext.MarketPlaces
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == HbMarketPlaceId);

            if (marketplace == null)
                return new ErrorResult("Hepsiburada marketplace kaydı bulunamadı.");

            var merchantId = marketplace.SellerId ?? "";

            var request = new HepsiburadaPreMatchApprovalRequest(merchantId, merchantSku);
            var response = await apiClient.PostAsync("/api/products/approve-prematch", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                logger.LogError("HB approve-prematch failed: {Status} {Body}", response.StatusCode, errorBody);
                return new ErrorResult($"Onay hatası: {response.StatusCode}");
            }

            logger.LogInformation("HB PRE_MATCHED approved: {MerchantSku}", merchantSku);
            return new SuccessResult("PRE_MATCHED ürün onaylandı.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HB approve-prematch exception for {MerchantSku}", merchantSku);
            return new ErrorResult($"Hata: {ex.Message}");
        }
    }
}

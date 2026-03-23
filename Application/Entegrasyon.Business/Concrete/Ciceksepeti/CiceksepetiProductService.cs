using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.Concrete.Ciceksepeti;

/// <summary>
/// Çiçeksepeti ürün publish servisi.
/// Pipeline: MappingValidation → Map → POST/PUT Products → batchId
/// </summary>
public sealed class CiceksepetiProductService(
    ICiceksepetiApiClient apiClient,
    ICiceksepetiProductMapper productMapper,
    CiceksepetiMappingValidator mappingValidator,
    IProductActivityLogger activityLogger,
    IApplicationLogManager applicationLogManager,
    ILogger<CiceksepetiProductService> logger,
    IDbContextFactory<IntegrationDbContext> contextFactory) : ICiceksepetiProductService
{
    private const string MarketplaceName = "Çiçeksepeti";
    private const string ProductsEndpoint = "Products";

    // ── PublishProductAsync ───────────────────────────────────────────────────

    public async Task<IDataResult<string>> PublishProductAsync(
        Guid productId, CancellationToken ct = default)
    {
        // 1. Mapping validation
        var validationResult = await mappingValidator.ValidateProductMappingsAsync(productId);
        await activityLogger.LogAsync(productId, ProductActivityType.MappingValidated,
            validationResult.Success ? "Çiçeksepeti mapping doğrulaması başarılı" : validationResult.Message,
            validationResult.Success ? ProductActivityStatus.Success : ProductActivityStatus.Error,
            marketplaceName: MarketplaceName);

        if (!validationResult.Success)
        {
            logger.LogWarning("Çiçeksepeti mapping validation failed for {ProductId}: {Message}",
                productId, validationResult.Message);
            return new ErrorDataResult<string>(null, validationResult.Message);
        }

        // 2. Mapping
        var mapResult = await productMapper.MapToCreateRequestAsync(productId, ct);
        if (!mapResult.Success)
        {
            logger.LogWarning("Çiçeksepeti product mapping failed for {ProductId}: {Message}",
                productId, mapResult.Message);
            return new ErrorDataResult<string>(null, mapResult.Message);
        }

        // 3. Publish
        try
        {
            var response = await apiClient.PostAsync(ProductsEndpoint, mapResult.Data!, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogError("Çiçeksepeti publish failed: {Status} {Body}", response.StatusCode, errorBody);

                await activityLogger.LogAsync(productId, ProductActivityType.BatchFailed,
                    $"Çiçeksepeti API hatası: {response.StatusCode}",
                    ProductActivityStatus.Error, errorBody, MarketplaceName);

                await applicationLogManager.AddLog(
                    $"Çiçeksepeti ürün gönderimi başarısız: {response.StatusCode}",
                    Entity.Logs.LogType.Product, Entity.Logs.LogAction.None, null, ct);

                return new ErrorDataResult<string>(null, $"API hatası: {response.StatusCode}");
            }

            var batchResponse = await response.Content
                .ReadFromJsonAsync<CiceksepetiBatchResponse>(cancellationToken: ct);

            var batchId = batchResponse?.BatchId;
            if (string.IsNullOrEmpty(batchId))
            {
                const string msg = "Çiçeksepeti'den batchId alınamadı.";
                logger.LogWarning(msg + " ProductId={ProductId}", productId);
                return new ErrorDataResult<string>(null, msg);
            }

            await activityLogger.LogAsync(productId, ProductActivityType.PublishSent,
                "Çiçeksepeti'ye ürün gönderildi",
                ProductActivityStatus.Success, null, MarketplaceName, batchId);

            await applicationLogManager.AddLog(
                $"Çiçeksepeti ürün gönderildi. BatchId: {batchId}",
                Entity.Logs.LogType.Product, Entity.Logs.LogAction.None, null, ct);

            logger.LogInformation("Çiçeksepeti publish success: {ProductId}, batchId={BatchId}", productId, batchId);
            return new SuccessDataResult<string>(batchId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Çiçeksepeti publish exception for {ProductId}", productId);
            await activityLogger.LogAsync(productId, ProductActivityType.BatchFailed,
                $"Çiçeksepeti publish hatası: {ex.Message}",
                ProductActivityStatus.Error, ex.ToString(), MarketplaceName);
            return new ErrorDataResult<string>(null, $"Hata: {ex.Message}");
        }
    }

    // ── UpdateProductAsync ────────────────────────────────────────────────────

    public async Task<IDataResult<string>> UpdateProductAsync(
        Guid productId, CancellationToken ct = default)
    {
        // 1. Mapping validation
        var validationResult = await mappingValidator.ValidateProductMappingsAsync(productId);
        await activityLogger.LogAsync(productId, ProductActivityType.MappingValidated,
            validationResult.Success ? "Çiçeksepeti mapping doğrulaması başarılı" : validationResult.Message,
            validationResult.Success ? ProductActivityStatus.Success : ProductActivityStatus.Error,
            marketplaceName: MarketplaceName);

        if (!validationResult.Success)
        {
            logger.LogWarning("Çiçeksepeti mapping validation failed for {ProductId}: {Message}",
                productId, validationResult.Message);
            return new ErrorDataResult<string>(null, validationResult.Message);
        }

        // 2. Mapping (update = isActive=true)
        var mapResult = await productMapper.MapToUpdateRequestAsync(productId, ct);
        if (!mapResult.Success)
        {
            logger.LogWarning("Çiçeksepeti product mapping (update) failed for {ProductId}: {Message}",
                productId, mapResult.Message);
            return new ErrorDataResult<string>(null, mapResult.Message);
        }

        // 3. Update (PUT)
        try
        {
            var response = await apiClient.PutAsync(ProductsEndpoint, mapResult.Data!, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogError("Çiçeksepeti update failed: {Status} {Body}", response.StatusCode, errorBody);

                await activityLogger.LogAsync(productId, ProductActivityType.BatchFailed,
                    $"Çiçeksepeti güncelleme API hatası: {response.StatusCode}",
                    ProductActivityStatus.Error, errorBody, MarketplaceName);

                await applicationLogManager.AddLog(
                    $"Çiçeksepeti ürün güncellemesi başarısız: {response.StatusCode}",
                    Entity.Logs.LogType.Product, Entity.Logs.LogAction.None, null, ct);

                return new ErrorDataResult<string>(null, $"API hatası: {response.StatusCode}");
            }

            var batchResponse = await response.Content
                .ReadFromJsonAsync<CiceksepetiBatchResponse>(cancellationToken: ct);

            var batchId = batchResponse?.BatchId;
            if (string.IsNullOrEmpty(batchId))
            {
                const string msg = "Çiçeksepeti güncellemeden batchId alınamadı.";
                logger.LogWarning(msg + " ProductId={ProductId}", productId);
                return new ErrorDataResult<string>(null, msg);
            }

            await activityLogger.LogAsync(productId, ProductActivityType.ContentUpdated,
                "Çiçeksepeti'de ürün güncellendi",
                ProductActivityStatus.Success, null, MarketplaceName, batchId);

            await applicationLogManager.AddLog(
                $"Çiçeksepeti ürün güncellendi. BatchId: {batchId}",
                Entity.Logs.LogType.Product, Entity.Logs.LogAction.None, null, ct);

            logger.LogInformation("Çiçeksepeti update success: {ProductId}, batchId={BatchId}", productId, batchId);
            return new SuccessDataResult<string>(batchId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Çiçeksepeti update exception for {ProductId}", productId);
            await activityLogger.LogAsync(productId, ProductActivityType.BatchFailed,
                $"Çiçeksepeti güncelleme hatası: {ex.Message}",
                ProductActivityStatus.Error, ex.ToString(), MarketplaceName);
            return new ErrorDataResult<string>(null, $"Hata: {ex.Message}");
        }
    }

    // ── CheckBatchStatusAsync ─────────────────────────────────────────────────

    public async Task<IDataResult<CiceksepetiBatchStatusResponse>> CheckBatchStatusAsync(
        string batchId, CancellationToken ct = default)
    {
        try
        {
            var response = await apiClient.GetAsync($"Products/batch-status/{batchId}", ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("Çiçeksepeti batch status failed: {Status} {Body}", response.StatusCode, errorBody);
                return new ErrorDataResult<CiceksepetiBatchStatusResponse>(null, $"Status API hatası: {response.StatusCode}");
            }

            var statusResponse = await response.Content
                .ReadFromJsonAsync<CiceksepetiBatchStatusResponse>(cancellationToken: ct);

            if (statusResponse is null)
                return new ErrorDataResult<CiceksepetiBatchStatusResponse>(null, "Batch status bilgisi alınamadı.");

            logger.LogInformation("Çiçeksepeti batch status: {BatchId}, {ItemCount} items", batchId, statusResponse.ItemCount);
            return new SuccessDataResult<CiceksepetiBatchStatusResponse>(statusResponse);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Çiçeksepeti batch status exception for {BatchId}", batchId);
            return new ErrorDataResult<CiceksepetiBatchStatusResponse>(null, $"Hata: {ex.Message}");
        }
    }

    // ── GetProductsAsync ──────────────────────────────────────────────────────

    public async Task<IDataResult<CiceksepetiProductListResponse>> GetProductsAsync(
        int page = 1, int pageSize = 60, int? statusFilter = null, CancellationToken ct = default)
    {
        try
        {
            // Çiçeksepeti uses 1-based pagination
            var url = $"Products?Page={page}&PageSize={pageSize}";
            if (statusFilter.HasValue)
                url += $"&ProductStatus={statusFilter.Value}";

            var response = await apiClient.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("Çiçeksepeti GetProducts failed: {Status} {Body}", response.StatusCode, errorBody);
                return new ErrorDataResult<CiceksepetiProductListResponse>(null, $"API hatası: {response.StatusCode}");
            }

            var listResponse = await response.Content
                .ReadFromJsonAsync<CiceksepetiProductListResponse>(cancellationToken: ct);

            if (listResponse is null)
                return new ErrorDataResult<CiceksepetiProductListResponse>(null, "Ürün listesi alınamadı.");

            logger.LogInformation("Çiçeksepeti GetProducts: page={Page}, total={Total}", page, listResponse.TotalCount);
            return new SuccessDataResult<CiceksepetiProductListResponse>(listResponse);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Çiçeksepeti GetProducts exception");
            return new ErrorDataResult<CiceksepetiProductListResponse>(null, $"Hata: {ex.Message}");
        }
    }
}

using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Trendyol;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Trendyol;

public sealed class TrendyolProductService(
    IntegrationDbContext dbContext,
    ITrendyolApiClient apiClient,
    ITrendyolProductMapper productMapper,
    TrendyolMappingValidator mappingValidator,
    IProductActivityLogger activityLogger,
    ILogger<TrendyolProductService> logger) : ITrendyolProductService
{
    private const int TrendyolMarketPlaceId = 1;

    public async Task<IDataResult<string>> PublishProductAsync(Guid productId)
    {
        var validationResult = await mappingValidator.ValidateProductMappingsAsync(productId);
        if (!validationResult.Success)
        {
            await activityLogger.LogAsync(productId, ProductActivityType.MappingValidated,
                $"Eşleştirme doğrulaması başarısız: {validationResult.Message}",
                ProductActivityStatus.Error, marketplaceName: "Trendyol");
            return new ErrorDataResult<string>(null!, validationResult.Message);
        }

        await activityLogger.LogAsync(productId, ProductActivityType.MappingValidated,
            "Eşleştirme doğrulaması başarılı", ProductActivityStatus.Success, marketplaceName: "Trendyol");

        var mapResult = await productMapper.MapProductAsync(productId);
        if (!mapResult.Success)
        {
            await activityLogger.LogAsync(productId, ProductActivityType.PublishRequested,
                $"Ürün mapping hatası: {mapResult.Message}",
                ProductActivityStatus.Error, marketplaceName: "Trendyol");
            return new ErrorDataResult<string>(null!, mapResult.Message);
        }

        var marketplace = await dbContext.MarketPlaces.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == TrendyolMarketPlaceId);

        if (marketplace?.SellerId is null)
            return new ErrorDataResult<string>(null!, "Trendyol SellerId ayarlanmamış.");

        var url = $"integration/product/sellers/{marketplace.SellerId}/v2/products";
        var response = await apiClient.PostAsync(url, mapResult.Data);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            logger.LogError("Trendyol product publish failed. Status={Status}, Body={Body}",
                response.StatusCode, errorBody);

            await activityLogger.LogAsync(productId, ProductActivityType.PublishSent,
                $"Trendyol API hatası: {response.StatusCode}",
                ProductActivityStatus.Error, detail: errorBody, marketplaceName: "Trendyol");

            return new ErrorDataResult<string>(null!, $"Trendyol API hatası: {response.StatusCode} — {errorBody}");
        }

        var batchResponse = await response.Content.ReadFromJsonAsync<TrendyolBatchResponse>();
        var batchRequestId = batchResponse?.BatchRequestId
            ?? throw new InvalidOperationException("Trendyol batch response'da BatchRequestId bulunamadı.");

        var pm = await dbContext.ProductMarketplaces
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.MarketPlaceId == TrendyolMarketPlaceId);

        if (pm is not null)
        {
            pm.BatchRequestId = batchRequestId;
            pm.StatusMessage = null;
            await dbContext.SaveChangesAsync();
        }

        await activityLogger.LogAsync(productId, ProductActivityType.PublishSent,
            $"Trendyol'a gönderildi — {mapResult.Data.Items.Count} varyant",
            ProductActivityStatus.Success, marketplaceName: "Trendyol", referenceId: batchRequestId);

        return new SuccessDataResult<string>(batchRequestId, "Ürün Trendyol'a gönderildi.");
    }

    public async Task<IDataResult<TrendyolBatchStatusResponse>> CheckBatchStatusAsync(string batchRequestId)
    {
        var marketplace = await dbContext.MarketPlaces.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == TrendyolMarketPlaceId);

        if (marketplace?.SellerId is null)
            return new ErrorDataResult<TrendyolBatchStatusResponse>(null!, "Trendyol SellerId ayarlanmamış.");

        var url = $"integration/product/sellers/{marketplace.SellerId}/products/batch-requests/{batchRequestId}";
        var response = await apiClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            logger.LogError("Trendyol batch status check failed. BatchId={BatchId}, Status={Status}",
                batchRequestId, response.StatusCode);
            return new ErrorDataResult<TrendyolBatchStatusResponse>(null!, $"Trendyol API hatası: {response.StatusCode}");
        }

        var statusResponse = await response.Content.ReadFromJsonAsync<TrendyolBatchStatusResponse>();
        if (statusResponse is null)
            return new ErrorDataResult<TrendyolBatchStatusResponse>(null!, "Trendyol batch status response parse edilemedi.");

        return new SuccessDataResult<TrendyolBatchStatusResponse>(statusResponse);
    }

    public async Task<IResult> UpdateUnapprovedProductAsync(Guid productId)
    {
        var mapResult = await productMapper.MapProductAsync(productId);
        if (!mapResult.Success)
            return new ErrorResult(mapResult.Message);

        var marketplace = await dbContext.MarketPlaces.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == TrendyolMarketPlaceId);

        if (marketplace?.SellerId is null)
            return new ErrorResult("Trendyol SellerId ayarlanmamış.");

        var url = $"integration/product/sellers/{marketplace.SellerId}/v2/products/unapproved-bulk-update";
        var response = await apiClient.PutAsync(url, mapResult.Data);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            await activityLogger.LogAsync(productId, ProductActivityType.ContentUpdated,
                $"Onaysız ürün güncelleme hatası: {response.StatusCode}",
                ProductActivityStatus.Error, detail: errorBody, marketplaceName: "Trendyol");
            return new ErrorResult($"Güncelleme hatası: {response.StatusCode}");
        }

        await activityLogger.LogAsync(productId, ProductActivityType.ContentUpdated,
            "Onaysız ürün Trendyol'da güncellendi",
            ProductActivityStatus.Success, marketplaceName: "Trendyol");

        return new SuccessResult("Onaysız ürün güncellendi.");
    }

    public async Task<IResult> UpdateApprovedContentAsync(Guid productId)
    {
        var pm = await dbContext.ProductMarketplaces.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.MarketPlaceId == TrendyolMarketPlaceId);

        if (pm?.ContentId is null)
            return new ErrorResult("ContentId bulunamadı — ürün henüz onaylanmamış olabilir.");

        var mapResult = await productMapper.MapProductAsync(productId);
        if (!mapResult.Success)
            return new ErrorResult(mapResult.Message);

        var marketplace = await dbContext.MarketPlaces.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == TrendyolMarketPlaceId);

        if (marketplace?.SellerId is null)
            return new ErrorResult("Trendyol SellerId ayarlanmamış.");

        var url = $"integration/product/sellers/{marketplace.SellerId}/v2/products/content-bulk-update";
        var response = await apiClient.PutAsync(url, mapResult.Data);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            await activityLogger.LogAsync(productId, ProductActivityType.ContentUpdated,
                $"Onaylı ürün içerik güncelleme hatası: {response.StatusCode}",
                ProductActivityStatus.Error, detail: errorBody, marketplaceName: "Trendyol");
            return new ErrorResult($"İçerik güncelleme hatası: {response.StatusCode}");
        }

        await activityLogger.LogAsync(productId, ProductActivityType.ContentUpdated,
            $"Onaylı ürün içeriği Trendyol'da güncellendi (contentId={pm.ContentId})",
            ProductActivityStatus.Success, marketplaceName: "Trendyol", referenceId: pm.ContentId.ToString());

        return new SuccessResult("Onaylı ürün içeriği güncellendi.");
    }
}

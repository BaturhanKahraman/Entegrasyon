using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Trendyol;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Trendyol;

/// <summary>
/// Gerçek Trendyol API çağrıları yapan servis.
/// POST V2 product endpoint'i ve batch status polling.
/// </summary>
public sealed class TrendyolProductService(
    IntegrationDbContext dbContext,
    ITrendyolApiClient apiClient,
    ITrendyolProductMapper productMapper,
    TrendyolMappingValidator mappingValidator,
    ILogger<TrendyolProductService> logger) : ITrendyolProductService
{
    private const int TrendyolMarketPlaceId = 1;

    public async Task<IDataResult<string>> PublishProductAsync(Guid productId)
    {
        // 1. Eşleştirme doğrulama
        var validationResult = await mappingValidator.ValidateProductMappingsAsync(productId);
        if (!validationResult.Success)
            return new ErrorDataResult<string>(null!, validationResult.Message);

        // 2. Product → TrendyolProductItem mapping
        var mapResult = await productMapper.MapProductAsync(productId);
        if (!mapResult.Success)
            return new ErrorDataResult<string>(null!, mapResult.Message);

        // 3. SellerId'yi al
        var marketplace = await dbContext.MarketPlaces.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == TrendyolMarketPlaceId);

        if (marketplace?.SellerId is null)
            return new ErrorDataResult<string>(null!, "Trendyol SellerId ayarlanmamış. Marketplace ayarlarını kontrol edin.");

        // 4. Trendyol API çağrısı
        var url = $"integration/product/sellers/{marketplace.SellerId}/v2/products";
        var response = await apiClient.PostAsync(url, mapResult.Data);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            logger.LogError("Trendyol product publish failed. Status={Status}, Body={Body}",
                response.StatusCode, errorBody);
            return new ErrorDataResult<string>(null!, $"Trendyol API hatası: {response.StatusCode} — {errorBody}");
        }

        var batchResponse = await response.Content.ReadFromJsonAsync<TrendyolBatchResponse>();
        var batchRequestId = batchResponse?.BatchRequestId
            ?? throw new InvalidOperationException("Trendyol batch response'da BatchRequestId bulunamadı.");

        // 5. ProductMarketplace kaydını güncelle
        var pm = await dbContext.ProductMarketplaces
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.MarketPlaceId == TrendyolMarketPlaceId);

        if (pm is not null)
        {
            pm.BatchRequestId = batchRequestId;
            pm.StatusMessage = null;
            await dbContext.SaveChangesAsync();
        }

        logger.LogInformation("Product {ProductId} published to Trendyol. BatchRequestId={BatchId}",
            productId, batchRequestId);

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
            logger.LogError("Trendyol unapproved update failed. ProductId={ProductId}, Body={Body}",
                productId, errorBody);
            return new ErrorResult($"Güncelleme hatası: {response.StatusCode}");
        }

        logger.LogInformation("Unapproved product {ProductId} updated on Trendyol", productId);
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

        // Content update: her item'a contentId eklenmeli
        // Trendyol content-bulk-update endpoint'i aynı body formatını kullanır ama contentId zorunlu
        var marketplace = await dbContext.MarketPlaces.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == TrendyolMarketPlaceId);

        if (marketplace?.SellerId is null)
            return new ErrorResult("Trendyol SellerId ayarlanmamış.");

        var url = $"integration/product/sellers/{marketplace.SellerId}/v2/products/content-bulk-update";
        var response = await apiClient.PutAsync(url, mapResult.Data);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            logger.LogError("Trendyol content update failed. ProductId={ProductId}, Body={Body}",
                productId, errorBody);
            return new ErrorResult($"İçerik güncelleme hatası: {response.StatusCode}");
        }

        logger.LogInformation("Approved product {ProductId} content updated on Trendyol (contentId={ContentId})",
            productId, pm.ContentId);
        return new SuccessResult("Onaylı ürün içeriği güncellendi.");
    }
}

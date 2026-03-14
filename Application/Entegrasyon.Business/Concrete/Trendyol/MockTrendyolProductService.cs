using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Trendyol;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Trendyol;

/// <summary>
/// Mock Trendyol servisi — gerçek HTTP çağrısı yapmaz ama mapper'ı çalıştırarak
/// mapping hatalarının local'de de yakalanmasını sağlar.
/// </summary>
public sealed class MockTrendyolProductService(
    IntegrationDbContext dbContext,
    ITrendyolProductMapper productMapper,
    TrendyolMappingValidator mappingValidator,
    ILogger<MockTrendyolProductService> logger) : ITrendyolProductService
{
    private const int TrendyolMarketPlaceId = 1;

    public async Task<IDataResult<string>> PublishProductAsync(Guid productId)
    {
        // 1. Eşleştirme doğrulama
        var validationResult = await mappingValidator.ValidateProductMappingsAsync(productId);
        if (!validationResult.Success)
            return new ErrorDataResult<string>(null!, validationResult.Message);

        // 2. Mapper'ı çalıştır — mock'ta bile mapping hataları yakalansın
        var mapResult = await productMapper.MapProductAsync(productId);
        if (!mapResult.Success)
            return new ErrorDataResult<string>(null!, mapResult.Message);

        // 3. Request body'yi logla (debug için)
        logger.LogInformation("Mock: Trendyol request body:\n{RequestBody}",
            JsonSerializer.Serialize(mapResult.Data, new JsonSerializerOptions { WriteIndented = true }));

        // 4. Mock: Batch request simülasyonu
        var batchRequestId = $"mock-batch-{Guid.NewGuid():N}";
        await Task.Delay(300);

        // 5. ProductMarketplace kaydını güncelle
        var marketplace = await dbContext.ProductMarketplaces
            .FirstOrDefaultAsync(pm => pm.ProductId == productId && pm.MarketPlaceId == TrendyolMarketPlaceId);

        if (marketplace is not null)
        {
            marketplace.BatchRequestId = batchRequestId;
            marketplace.StatusMessage = null;
            await dbContext.SaveChangesAsync();
        }

        logger.LogInformation(
            "Mock: Product {ProductId} published to Trendyol. BatchRequestId={BatchId}, Items={ItemCount}",
            productId, batchRequestId, mapResult.Data.Items.Count);

        return new SuccessDataResult<string>(batchRequestId, "Ürün Trendyol'a gönderildi (mock).");
    }

    public Task<IDataResult<TrendyolBatchStatusResponse>> CheckBatchStatusAsync(string batchRequestId)
    {
        // Mock: Her zaman COMPLETED döndür
        var response = new TrendyolBatchStatusResponse(
            batchRequestId,
            TrendyolBatchStatus.COMPLETED,
            Items: null,
            ItemCount: 1,
            FailedItemCount: 0,
            BatchRequestType: "ProductV2OnBoarding",
            CreationDate: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            LastModification: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        logger.LogInformation("Mock: Batch {BatchId} status checked — COMPLETED", batchRequestId);

        return Task.FromResult<IDataResult<TrendyolBatchStatusResponse>>(
            new SuccessDataResult<TrendyolBatchStatusResponse>(response));
    }

    public async Task<IResult> UpdateUnapprovedProductAsync(Guid productId)
    {
        var mapResult = await productMapper.MapProductAsync(productId);
        if (!mapResult.Success)
            return new ErrorResult(mapResult.Message);

        logger.LogInformation("Mock: Unapproved product {ProductId} updated with {ItemCount} items",
            productId, mapResult.Data.Items.Count);
        return new SuccessResult("Onaysız ürün güncellendi (mock).");
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

        logger.LogInformation("Mock: Approved product {ProductId} content updated (contentId={ContentId})",
            productId, pm.ContentId);
        return new SuccessResult("Onaylı ürün içeriği güncellendi (mock).");
    }
}

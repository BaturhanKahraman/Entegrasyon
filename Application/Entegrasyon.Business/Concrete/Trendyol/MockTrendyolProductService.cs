using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Trendyol;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Trendyol;

public sealed class MockTrendyolProductService(
    IntegrationDbContext dbContext,
    ITrendyolProductMapper productMapper,
    TrendyolMappingValidator mappingValidator,
    IProductActivityLogger activityLogger,
    ILogger<MockTrendyolProductService> logger) : ITrendyolProductService
{
    private const int TrendyolMarketPlaceId = 1;

    public async Task<IDataResult<string>> PublishProductAsync(Guid productId)
    {
        // 1. Eşleştirme doğrulama
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

        // 2. Mapper'ı çalıştır
        var mapResult = await productMapper.MapProductAsync(productId);
        if (!mapResult.Success)
        {
            await activityLogger.LogAsync(productId, ProductActivityType.PublishRequested,
                $"Ürün mapping hatası: {mapResult.Message}",
                ProductActivityStatus.Error, marketplaceName: "Trendyol");
            return new ErrorDataResult<string>(null!, mapResult.Message);
        }

        logger.LogInformation("Mock: Trendyol request body:\n{RequestBody}",
            JsonSerializer.Serialize(mapResult.Data, new JsonSerializerOptions { WriteIndented = true }));

        // 3. Mock batch request
        var batchRequestId = $"mock-batch-{Guid.NewGuid():N}";
        await Task.Delay(300);

        // 4. ProductMarketplace kaydını güncelle
        var marketplace = await dbContext.ProductMarketplaces
            .FirstOrDefaultAsync(pm => pm.ProductId == productId && pm.MarketPlaceId == TrendyolMarketPlaceId);

        if (marketplace is not null)
        {
            marketplace.BatchRequestId = batchRequestId;
            marketplace.StatusMessage = null;
            await dbContext.SaveChangesAsync();
        }

        await activityLogger.LogAsync(productId, ProductActivityType.PublishSent,
            $"Trendyol'a gönderildi — {mapResult.Data.Items.Count} varyant (mock)",
            ProductActivityStatus.Success, marketplaceName: "Trendyol", referenceId: batchRequestId);

        logger.LogInformation(
            "Mock: Product {ProductId} published to Trendyol. BatchRequestId={BatchId}, Items={ItemCount}",
            productId, batchRequestId, mapResult.Data.Items.Count);

        return new SuccessDataResult<string>(batchRequestId, "Ürün Trendyol'a gönderildi (mock).");
    }

    public Task<IDataResult<TrendyolBatchStatusResponse>> CheckBatchStatusAsync(string batchRequestId)
    {
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

        await activityLogger.LogAsync(productId, ProductActivityType.ContentUpdated,
            $"Onaysız ürün güncellendi — {mapResult.Data.Items.Count} varyant (mock)",
            ProductActivityStatus.Success, marketplaceName: "Trendyol");

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

        await activityLogger.LogAsync(productId, ProductActivityType.ContentUpdated,
            $"Onaylı ürün içeriği güncellendi (contentId={pm.ContentId}) (mock)",
            ProductActivityStatus.Success, marketplaceName: "Trendyol", referenceId: pm.ContentId.ToString());

        return new SuccessResult("Onaylı ürün içeriği güncellendi (mock).");
    }
}

using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Trendyol;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.Concrete.Trendyol;

public sealed class MockTrendyolProductService(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    ITrendyolProductMapper productMapper,
    TrendyolMappingValidator mappingValidator,
    IProductActivityLogger activityLogger,
    ILogger<MockTrendyolProductService> logger) : ITrendyolProductService
{
    public async Task<IDataResult<string>> PublishProductAsync(Guid productId)
    {
        // 1. Eslestirme dogrulama
        var validationResult = await mappingValidator.ValidateProductMappingsAsync(productId);
        if (!validationResult.Success)
        {
            await activityLogger.LogAsync(productId, ProductActivityType.MappingValidated,
                $"Eslestirme dogrulamasi basarisiz: {validationResult.Message}",
                ProductActivityStatus.Error, marketplaceName: "Trendyol");
            return new ErrorDataResult<string>(null!, validationResult.Message!);
        }

        await activityLogger.LogAsync(productId, ProductActivityType.MappingValidated,
            "Eslestirme dogrulamasi basarili", ProductActivityStatus.Success, marketplaceName: "Trendyol");

        // 2. Mapper'i calistir
        var mapResult = await productMapper.MapProductAsync(productId);
        if (!mapResult.Success)
        {
            await activityLogger.LogAsync(productId, ProductActivityType.PublishRequested,
                $"Urun mapping hatasi: {mapResult.Message}",
                ProductActivityStatus.Error, marketplaceName: "Trendyol");
            return new ErrorDataResult<string>(null!, mapResult.Message!);
        }

        logger.LogInformation("Mock: Trendyol request body:\n{RequestBody}",
            JsonSerializer.Serialize(mapResult.Data, new JsonSerializerOptions { WriteIndented = true }));

        // 3. Mock batch request
        var batchRequestId = $"mock-batch-{Guid.NewGuid():N}";
        await Task.Delay(300);

        // 4. ProductMarketplace kaydini guncelle
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var marketplace = await dbContext.ProductMarketplaces
            .FirstOrDefaultAsync(pm => pm.ProductId == productId && pm.MarketPlaceId == TrendyolMarketPlaceId);

        if (marketplace is not null)
        {
            marketplace.BatchRequestId = batchRequestId;
            marketplace.StatusMessage = null;
            await dbContext.SaveChangesAsync();
        }

        await activityLogger.LogAsync(productId, ProductActivityType.PublishSent,
            $"Trendyol'a gonderildi -- {mapResult.Data.Items.Count} varyant (mock)",
            ProductActivityStatus.Success, marketplaceName: "Trendyol", referenceId: batchRequestId);

        logger.LogInformation(
            "Mock: Product {ProductId} published to Trendyol. BatchRequestId={BatchId}, Items={ItemCount}",
            productId, batchRequestId, mapResult.Data.Items.Count);

        return new SuccessDataResult<string>(batchRequestId, "Urun Trendyol'a gonderildi (mock).");
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

        logger.LogInformation("Mock: Batch {BatchId} status checked -- COMPLETED", batchRequestId);

        return Task.FromResult<IDataResult<TrendyolBatchStatusResponse>>(
            new SuccessDataResult<TrendyolBatchStatusResponse>(response));
    }

    public async Task<IResult> UpdateUnapprovedProductAsync(Guid productId)
    {
        var mapResult = await productMapper.MapProductAsync(productId);
        if (!mapResult.Success)
            return new ErrorResult(mapResult.Message!);

        await activityLogger.LogAsync(productId, ProductActivityType.ContentUpdated,
            $"Onaysiz urun guncellendi -- {mapResult.Data.Items.Count} varyant (mock)",
            ProductActivityStatus.Success, marketplaceName: "Trendyol");

        return new SuccessResult("Onaysiz urun guncellendi (mock).");
    }

    public async Task<IResult> UpdateApprovedContentAsync(Guid productId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var pm = await dbContext.ProductMarketplaces.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.MarketPlaceId == TrendyolMarketPlaceId);

        if (pm?.ContentId is null)
            return new ErrorResult("ContentId bulunamadi -- urun henuz onaylanmamis olabilir.");

        var mapResult = await productMapper.MapProductAsync(productId);
        if (!mapResult.Success)
            return new ErrorResult(mapResult.Message!);

        await activityLogger.LogAsync(productId, ProductActivityType.ContentUpdated,
            $"Onayli urun icerigi guncellendi (contentId={pm.ContentId}) (mock)",
            ProductActivityStatus.Success, marketplaceName: "Trendyol", referenceId: pm.ContentId.ToString());

        return new SuccessResult("Onayli urun icerigi guncellendi (mock).");
    }
}

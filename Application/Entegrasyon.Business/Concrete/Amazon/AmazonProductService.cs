using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Amazon;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Amazon;

/// <summary>
/// Amazon ürün publish orchestrator. Pipeline: Validator → Mapper → ListingService.PutListingItem
/// </summary>
public sealed class AmazonProductService(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IAmazonListingService listingService,
    IAmazonProductMapper productMapper,
    AmazonMappingValidator mappingValidator,
    IProductActivityLogger activityLogger,
    ILogger<AmazonProductService> logger) : IAmazonProductService
{
    private const int AmazonMpId = MarketPlaceConstants.AmazonMarketPlaceId;

    public async Task<IDataResult<string>> PublishProductAsync(Guid productId, CancellationToken ct = default)
    {
        // 1. Validation
        var validationResult = await mappingValidator.ValidateProductMappingsAsync(productId);
        await activityLogger.LogAsync(productId, ProductActivityType.MappingValidated,
            validationResult.Success ? "Amazon mapping doğrulaması başarılı" : validationResult.Message,
            validationResult.Success ? ProductActivityStatus.Success : ProductActivityStatus.Error,
            marketplaceName: "Amazon");

        if (!validationResult.Success)
            return new ErrorDataResult<string>(null, validationResult.Message);

        // 2. Mapping
        await using var dbContext = await contextFactory.CreateDbContextAsync(ct);
        var pm = await dbContext.ProductMarketplaces
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.MarketPlaceId == AmazonMpId, ct);

        var marketplace = await dbContext.MarketPlaces.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == AmazonMpId, ct);
        var sellerId = marketplace?.SellerId ?? "";

        // Product type from category marketplace mapping
        var product = await dbContext.MainProducts.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == productId, ct);
        var categoryMatch = await dbContext.CategoryMarketplaces.AsNoTracking()
            .FirstOrDefaultAsync(cm => cm.CategoryId == product!.CategoryId && cm.MarketPlaceId == AmazonMpId, ct);
        var productType = categoryMatch?.MarketPlaceCategoryName ?? "PRODUCT";

        var mapResult = await productMapper.MapProductAsync(productId, productType, ct);
        if (!mapResult.Success)
            return new ErrorDataResult<string>(null, mapResult.Message);

        // 3. Publish
        try
        {
            var sku = product!.StockCode ?? product.Id.ToString("N")[..16];
            var marketplaceIds = new[] { "A33AVAJ2PDY3EV" }; // TODO: config'den al

            var result = await listingService.PutListingItemAsync(sellerId, sku, mapResult.Data!, marketplaceIds, ct);

            if (!result.Success || result.Data == null)
                return new ErrorDataResult<string>(null, result.Message);

            if (result.Data.Status == "INVALID")
            {
                var issues = result.Data.Issues != null
                    ? string.Join(", ", result.Data.Issues.Select(i => i.Message))
                    : "Bilinmeyen validasyon hatası";
                if (pm != null) { pm.Status = MarketplaceProductStatus.Failed; pm.StatusMessage = issues; }
                await dbContext.SaveChangesAsync(ct);
                return new ErrorDataResult<string>(null, $"Amazon INVALID: {issues}");
            }

            // ACCEPTED
            if (pm != null)
            {
                pm.BatchRequestId = result.Data.SubmissionId;
                pm.ExternalProductId = sku;
                pm.UpdatedAt = DateTimeOffset.UtcNow;
            }
            await dbContext.SaveChangesAsync(ct);

            await activityLogger.LogAsync(productId, ProductActivityType.PublishSent,
                "Amazon'a ürün gönderildi (ACCEPTED)", ProductActivityStatus.Success,
                null, "Amazon", result.Data.SubmissionId);

            return new SuccessDataResult<string>(sku);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Amazon publish exception: {ProductId}", productId);
            return new ErrorDataResult<string>(null, $"Hata: {ex.Message}");
        }
    }

    public async Task<IDataResult<AmazonListingItemResponse>> CheckListingStatusAsync(string sku, CancellationToken ct = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(ct);
        var marketplace = await dbContext.MarketPlaces.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == AmazonMpId, ct);
        var sellerId = marketplace?.SellerId ?? "";
        return await listingService.GetListingItemAsync(sellerId, sku, new[] { "A33AVAJ2PDY3EV" }, ct);
    }

    public async Task<IResult> UpdateProductAsync(Guid productId, CancellationToken ct = default)
    {
        // PutListingItem ile full update (aynı publish flow)
        var result = await PublishProductAsync(productId, ct);
        return result.Success ? new SuccessResult("Ürün güncellendi.") : new ErrorResult(result.Message);
    }

    public async Task<IResult> DeleteProductAsync(Guid productId, CancellationToken ct = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(ct);
        var pm = await dbContext.ProductMarketplaces
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.MarketPlaceId == AmazonMpId, ct);
        if (pm?.ExternalProductId == null)
            return new ErrorResult("Amazon listing bulunamadı.");

        var marketplace = await dbContext.MarketPlaces.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == AmazonMpId, ct);
        return await listingService.DeleteListingItemAsync(
            marketplace?.SellerId ?? "", pm.ExternalProductId, new[] { "A33AVAJ2PDY3EV" }, ct);
    }
}

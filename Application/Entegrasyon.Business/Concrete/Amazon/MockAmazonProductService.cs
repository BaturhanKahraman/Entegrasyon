using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Amazon;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Amazon;

public sealed class MockAmazonProductService(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IAmazonProductMapper productMapper,
    AmazonMappingValidator mappingValidator,
    IProductActivityLogger activityLogger,
    ILogger<MockAmazonProductService> logger) : IAmazonProductService
{
    public async Task<IDataResult<string>> PublishProductAsync(Guid productId, CancellationToken ct = default)
    {
        var validationResult = await mappingValidator.ValidateProductMappingsAsync(productId);
        await activityLogger.LogAsync(productId, ProductActivityType.MappingValidated,
            validationResult.Success ? "Amazon mapping doğrulaması başarılı (MOCK)" : validationResult.Message!,
            validationResult.Success ? ProductActivityStatus.Success : ProductActivityStatus.Error,
            marketplaceName: "Amazon");

        if (!validationResult.Success)
            return new ErrorDataResult<string>(null!, validationResult.Message!);

        var mapResult = await productMapper.MapProductAsync(productId, "PRODUCT", ct);
        if (!mapResult.Success)
            return new ErrorDataResult<string>(null!, mapResult.Message!);

        var mockSku = $"MOCK-SKU-{productId.ToString("N")[..8]}";
        logger.LogInformation("[MOCK] Amazon publish: {ProductId} → {Sku}", productId, mockSku);

        await using var dbContext = await contextFactory.CreateDbContextAsync(ct);
        var pm = await dbContext.ProductMarketplaces
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.MarketPlaceId == MarketPlaceConstants.AmazonMarketPlaceId, ct);
        if (pm != null) { pm.ExternalProductId = mockSku; pm.UpdatedAt = DateTimeOffset.UtcNow; }
        await dbContext.SaveChangesAsync(ct);

        return new SuccessDataResult<string>(mockSku);
    }

    public Task<IDataResult<AmazonListingItemResponse>> CheckListingStatusAsync(string sku, CancellationToken ct = default)
    {
        var response = new AmazonListingItemResponse(sku,
            new List<AmazonListingSummary> { new("A33AVAJ2PDY3EV", "MOCK-ASIN", "PRODUCT", new List<string> { "BUYABLE" }, "Mock") },
            null, null, null);
        return Task.FromResult<IDataResult<AmazonListingItemResponse>>(new SuccessDataResult<AmazonListingItemResponse>(response));
    }

    public Task<IResult> UpdateProductAsync(Guid productId, CancellationToken ct = default) =>
        Task.FromResult<IResult>(new SuccessResult("Ürün güncellendi (MOCK)."));

    public Task<IResult> DeleteProductAsync(Guid productId, CancellationToken ct = default) =>
        Task.FromResult<IResult>(new SuccessResult("Listing silindi (MOCK)."));
}

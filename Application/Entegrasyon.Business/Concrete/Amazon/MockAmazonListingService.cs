using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Amazon;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Amazon;

public sealed class MockAmazonListingService(ILogger<MockAmazonListingService> logger) : IAmazonListingService
{
    public Task<IDataResult<AmazonListingSubmissionResponse>> PutListingItemAsync(
        string sellerId, string sku, AmazonListingItem item, string[] marketplaceIds, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Amazon putListingItem: {Sku}, productType={ProductType}", sku, item.ProductType);
        var response = new AmazonListingSubmissionResponse(sku, "ACCEPTED", $"mock-sub-{Guid.NewGuid():N}"[..24], null);
        return Task.FromResult<IDataResult<AmazonListingSubmissionResponse>>(new SuccessDataResult<AmazonListingSubmissionResponse>(response));
    }

    public Task<IDataResult<AmazonListingSubmissionResponse>> PatchListingItemAsync(
        string sellerId, string sku, AmazonListingPatchRequest patches, string[] marketplaceIds, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Amazon patchListingItem: {Sku}", sku);
        var response = new AmazonListingSubmissionResponse(sku, "ACCEPTED", $"mock-sub-{Guid.NewGuid():N}"[..24], null);
        return Task.FromResult<IDataResult<AmazonListingSubmissionResponse>>(new SuccessDataResult<AmazonListingSubmissionResponse>(response));
    }

    public Task<IDataResult<AmazonListingItemResponse>> GetListingItemAsync(
        string sellerId, string sku, string[] marketplaceIds, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Amazon getListingItem: {Sku}", sku);
        var response = new AmazonListingItemResponse(sku,
            new List<AmazonListingSummary> { new("A33AVAJ2PDY3EV", $"MOCK-ASIN-{sku}", "PRODUCT", new List<string> { "BUYABLE" }, "Mock Product") },
            null, null, null);
        return Task.FromResult<IDataResult<AmazonListingItemResponse>>(new SuccessDataResult<AmazonListingItemResponse>(response));
    }

    public Task<IResult> DeleteListingItemAsync(
        string sellerId, string sku, string[] marketplaceIds, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Amazon deleteListingItem: {Sku}", sku);
        return Task.FromResult<IResult>(new SuccessResult("Listing silindi (MOCK)."));
    }
}

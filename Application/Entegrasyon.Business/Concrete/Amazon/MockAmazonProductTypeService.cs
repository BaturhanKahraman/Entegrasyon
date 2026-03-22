using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Amazon;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Amazon;

public sealed class MockAmazonProductTypeService(ILogger<MockAmazonProductTypeService> logger) : IAmazonProductTypeService
{
    public Task<IDataResult<List<AmazonProductTypeSearchResult>>> SearchProductTypesAsync(
        string keywords, string marketplaceId, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Amazon product type search: {Keywords}", keywords);
        return Task.FromResult<IDataResult<List<AmazonProductTypeSearchResult>>>(
            new SuccessDataResult<List<AmazonProductTypeSearchResult>>(new List<AmazonProductTypeSearchResult>
            {
                new("PRODUCT", "Generic Product", new List<string> { marketplaceId })
            }));
    }

    public Task<IDataResult<AmazonProductTypeDefinition>> GetProductTypeDefinitionAsync(
        string productType, string marketplaceId, string? requirements = "LISTING", CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Amazon product type definition: {ProductType}", productType);
        return Task.FromResult<IDataResult<AmazonProductTypeDefinition>>(
            new SuccessDataResult<AmazonProductTypeDefinition>(
                new(productType, new("1.0", true), null, requirements, "ENFORCED", null)));
    }
}

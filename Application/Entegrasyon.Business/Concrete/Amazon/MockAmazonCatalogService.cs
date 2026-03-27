using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Amazon;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Amazon;

public sealed class MockAmazonCatalogService(ILogger<MockAmazonCatalogService> logger) : IAmazonCatalogService
{
    public Task<IDataResult<AmazonCatalogSearchResponse>> SearchCatalogItemsAsync(
        string keywords, string[] marketplaceIds, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Amazon catalog search: {Keywords}", keywords);
        return Task.FromResult<IDataResult<AmazonCatalogSearchResponse>>(
            new SuccessDataResult<AmazonCatalogSearchResponse>(new(0, null, [])));
    }

    public Task<IDataResult<AmazonCatalogItem>> GetCatalogItemAsync(
        string asin, string[] marketplaceIds, string[]? includedData = null, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Amazon get catalog item: {Asin}", asin);
        return Task.FromResult<IDataResult<AmazonCatalogItem>>(
            new ErrorDataResult<AmazonCatalogItem>(null!, "Mock: ASIN bulunamadı."));
    }

    public Task<IDataResult<AmazonCatalogSearchResponse>> SearchByIdentifierAsync(
        string identifier, string identifierType, string[] marketplaceIds, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Amazon identifier search: {Id} ({Type})", identifier, identifierType);
        return Task.FromResult<IDataResult<AmazonCatalogSearchResponse>>(
            new SuccessDataResult<AmazonCatalogSearchResponse>(new(0, null, [])));
    }
}

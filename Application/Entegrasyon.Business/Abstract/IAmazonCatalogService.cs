using Entegrasyon.Entity.Dtos.Amazon;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IAmazonCatalogService
{
    Task<IDataResult<AmazonCatalogSearchResponse>> SearchCatalogItemsAsync(
        string keywords, string[] marketplaceIds, CancellationToken ct = default);
    Task<IDataResult<AmazonCatalogItem>> GetCatalogItemAsync(
        string asin, string[] marketplaceIds, string[]? includedData = null, CancellationToken ct = default);
    Task<IDataResult<AmazonCatalogSearchResponse>> SearchByIdentifierAsync(
        string identifier, string identifierType, string[] marketplaceIds, CancellationToken ct = default);
}

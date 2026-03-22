using Entegrasyon.Entity.Dtos.Amazon;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IAmazonProductTypeService
{
    Task<IDataResult<List<AmazonProductTypeSearchResult>>> SearchProductTypesAsync(
        string keywords, string marketplaceId, CancellationToken ct = default);
    Task<IDataResult<AmazonProductTypeDefinition>> GetProductTypeDefinitionAsync(
        string productType, string marketplaceId, string? requirements = "LISTING", CancellationToken ct = default);
}

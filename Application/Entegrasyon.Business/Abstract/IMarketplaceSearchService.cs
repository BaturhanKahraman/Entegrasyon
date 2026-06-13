using Entegrasyon.Entity.Dtos.Marketplace;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IMarketplaceSearchService
{
    Task<IDataResult<List<MarketplaceCategorySearchResult>>> SearchCategoriesAsync(int marketPlaceId, string query, CancellationToken ct = default);
    Task<IDataResult<List<MarketplaceBrandSearchResult>>> SearchBrandsAsync(int marketPlaceId, string query, CancellationToken ct = default);
    Task<IDataResult<List<MarketplaceAttributeSearchResult>>> SearchAttributesAsync(int marketPlaceId, string query, CancellationToken ct = default);
    Task<IDataResult<List<MarketplaceOption>>> SearchAttributeValuesAsync(int marketPlaceId, int marketplaceAttributeId, string query, CancellationToken ct = default);
}

using Entegrasyon.Entity.Dtos.Marketplace;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IMarketplaceSearchService
{
    Task<IDataResult<List<MarketplaceCategorySearchResult>>> SearchCategoriesAsync(int marketPlaceId, string query, CancellationToken ct = default);
    Task<IDataResult<List<MarketplaceBrandSearchResult>>> SearchBrandsAsync(int marketPlaceId, string query, CancellationToken ct = default);
    Task<IDataResult<List<MarketplaceAttributeSearchResult>>> SearchAttributesAsync(int marketPlaceId, string query, CancellationToken ct = default);
    Task<IDataResult<List<MarketplaceOption>>> SearchAttributeValuesAsync(int marketPlaceId, int marketplaceAttributeId, string query, CancellationToken ct = default);

    /// <summary>
    /// Belirli bir pazaryeri kategorisi bağlamında özellik değeri araması — global birleştirme yapmaz.
    /// Aynı özelliğin farklı kategorilerdeki farklı değer setleri karışmaz.
    /// </summary>
    Task<IDataResult<List<MarketplaceOption>>> SearchAttributeValuesForCategoryAsync(
        int marketPlaceId, int marketplaceCategoryId, int marketplaceAttributeId, string query, CancellationToken ct = default);
}

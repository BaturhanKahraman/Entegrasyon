using Entegrasyon.Entity.Dtos.Marketplace;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IMarketplaceCategoryAttributeProvider
{
    int MarketPlaceId { get; }
    Task<IDataResult<List<MarketplaceAttributeDto>>> GetAttributesForCategoryAsync(
        int marketplaceCategoryId, CancellationToken ct = default);
}

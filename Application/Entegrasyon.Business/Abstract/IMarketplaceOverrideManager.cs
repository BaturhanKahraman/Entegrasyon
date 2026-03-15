using Entegrasyon.Entity.Dtos.Product.Marketplace;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IMarketplaceOverrideManager
{
    Task<IDataResult<MarketplaceOverrideDetailDto>> GetOverridesAsync(Guid productId, int marketPlaceId);
    Task<IResult> SaveOverridesAsync(SaveMarketplaceOverridesDto dto);
    Task<IResult> SaveOverridesAndPublishAsync(SaveMarketplaceOverridesDto dto);
}

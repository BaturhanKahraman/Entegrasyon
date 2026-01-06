using Entegrasyon.Entity.Matches;

namespace Entegrasyon.Business.Abstract;

public interface IBrandMatchService
{
    Task AddRange(List<BrandMarketPlaceMatch> entities);
    Task<List<int>> GetMarketPlaceBrandIdsByMarketPlaceId(int marketPlaceId);
}

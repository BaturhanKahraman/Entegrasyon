using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity.Matches;

namespace Entegrasyon.Business.Concrete;

public class BrandMatchService
{
    private readonly IBrandMarketPlaceMatchDal _dal;

    public BrandMatchService(IBrandMarketPlaceMatchDal dal)
    {
        _dal = dal;
    }

    public async Task AddRange(List<BrandMarketPlaceMatch> entities)
    {
        await _dal.AddRangeAsync(entities);
    }

    public Task<List<int>> GetMarketPlaceBrandIdsByMarketPlaceId(int marketPlaceId)=>
        _dal.GetTransformedEntitiesAsync(x=>x.MarketPlaceBrandId,null,expression :x=> x.MarketPlaceId == marketPlaceId);
    
}
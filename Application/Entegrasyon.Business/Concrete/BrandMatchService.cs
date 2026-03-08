using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Matches;
using Entegrasyon.Business.Abstract;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete;

public class BrandMatchService : IBrandMatchService
{
    private readonly IntegrationDbContext _dbContext;

    public BrandMatchService(IntegrationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddRange(List<BrandMarketPlaceMatch> entities)
    {
        _dbContext.BrandMarketPlaceMatches.AddRange(entities);
        await _dbContext.SaveChangesAsync();
    }

    public Task<List<int>> GetMarketPlaceBrandIdsByMarketPlaceId(int marketPlaceId) =>
        _dbContext.BrandMarketPlaceMatches
            .Where(x => x.MarketPlaceId == marketPlaceId)
            .Select(x => x.MarketPlaceBrandId)
            .ToListAsync();
}

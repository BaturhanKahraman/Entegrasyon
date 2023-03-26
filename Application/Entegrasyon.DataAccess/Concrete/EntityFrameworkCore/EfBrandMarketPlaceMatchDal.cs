using EFCore.BulkExtensions;
using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Matches;
using Microsoft.EntityFrameworkCore;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfBrandMarketPlaceMatchDal: EfEntityRepository<BrandMarketPlaceMatch,IntegrationDbContext>, IBrandMarketPlaceMatchDal
{
    private readonly IntegrationDbContext _context;
    public EfBrandMarketPlaceMatchDal(IntegrationDbContext ctx) : base(ctx)
    {
        _context = ctx;
    }

    public override async Task AddRange(List<BrandMarketPlaceMatch> entities)
    {
        await _context.BrandMarketPlaceMatches.AddRangeAsync(entities);
        await _context.SaveChangesAsync();
    }
}
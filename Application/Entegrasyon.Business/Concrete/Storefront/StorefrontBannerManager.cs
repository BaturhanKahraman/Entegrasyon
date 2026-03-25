using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete.Storefront;

public class StorefrontBannerManager(
    IDbContextFactory<IntegrationDbContext> contextFactory) : IStorefrontBannerManager
{
    public async Task<IDataResult<List<StorefrontBanner>>> GetActiveBannersAsync(int tenantId, BannerPosition position)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var now = DateTimeOffset.UtcNow;

        var banners = await dbContext.StorefrontBanners
            .AsNoTracking()
            .Where(b => b.TenantId == tenantId
                && b.Position == position
                && b.IsActive
                && (b.StartDate == null || b.StartDate <= now)
                && (b.EndDate == null || b.EndDate >= now))
            .OrderBy(b => b.DisplayOrder)
            .ToListAsync();

        return new SuccessDataResult<List<StorefrontBanner>>(banners);
    }
}

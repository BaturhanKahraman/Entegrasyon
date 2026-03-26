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

    public async Task<IDataResult<List<StorefrontBanner>>> GetAllBannersAsync(int tenantId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var banners = await dbContext.StorefrontBanners
            .AsNoTracking()
            .Where(b => b.TenantId == tenantId)
            .OrderBy(b => b.DisplayOrder)
            .ToListAsync();

        return new SuccessDataResult<List<StorefrontBanner>>(banners);
    }

    public async Task<IDataResult<StorefrontBanner>> CreateAsync(StorefrontBanner banner)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        await dbContext.StorefrontBanners.AddAsync(banner);
        await dbContext.SaveChangesAsync();

        return new SuccessDataResult<StorefrontBanner>(banner, "Banner olusturuldu.");
    }

    public async Task<IResult> UpdateAsync(StorefrontBanner banner)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var existing = await dbContext.StorefrontBanners
            .FirstOrDefaultAsync(b => b.Id == banner.Id);

        if (existing is null)
            return new ErrorResult("Banner bulunamadi.");

        existing.Title = banner.Title;
        existing.ImageUrl = banner.ImageUrl;
        existing.MobileImageUrl = banner.MobileImageUrl;
        existing.LinkUrl = banner.LinkUrl;
        existing.Position = banner.Position;
        existing.DisplayOrder = banner.DisplayOrder;
        existing.StartDate = banner.StartDate;
        existing.EndDate = banner.EndDate;
        existing.IsActive = banner.IsActive;

        dbContext.StorefrontBanners.Update(existing);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Banner guncellendi.");
    }

    public async Task<IResult> DeleteAsync(int id)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var banner = await dbContext.StorefrontBanners
            .FirstOrDefaultAsync(b => b.Id == id);

        if (banner is null)
            return new ErrorResult("Banner bulunamadi.");

        dbContext.StorefrontBanners.Remove(banner);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Banner silindi.");
    }
}

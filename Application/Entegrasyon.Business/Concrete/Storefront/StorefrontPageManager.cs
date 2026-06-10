using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete.Storefront;

public class StorefrontPageManager(
    IDbContextFactory<IntegrationDbContext> contextFactory) : IStorefrontPageManager
{
    public async Task<IDataResult<List<StorefrontPage>>> GetPublishedPagesAsync(int tenantId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var pages = await dbContext.StorefrontPages
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.IsPublished)
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync();

        return new SuccessDataResult<List<StorefrontPage>>(pages);
    }

    public async Task<IDataResult<StorefrontPage>> GetBySlugAsync(int tenantId, string slug)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var page = await dbContext.StorefrontPages
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Slug == slug && p.IsPublished);

        if (page is null)
            return new ErrorDataResult<StorefrontPage>(null!, "Sayfa bulunamadı.");

        return new SuccessDataResult<StorefrontPage>(page);
    }
}

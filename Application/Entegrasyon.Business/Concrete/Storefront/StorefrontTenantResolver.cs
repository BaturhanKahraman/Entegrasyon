using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Entegrasyon.Business.Concrete.Storefront;

public class StorefrontTenantResolver(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IMemoryCache cache) : IStorefrontTenantResolver
{
    private static string CacheKey(string hostname) => $"storefront:tenant:{hostname}";

    public async Task<StorefrontTenantInfo?> ResolveAsync(string hostname)
    {
        var key = CacheKey(hostname);

        if (cache.TryGetValue(key, out StorefrontTenantInfo? cached))
            return cached;

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var domain = await dbContext.StorefrontDomainMappings
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DomainName == hostname && d.IsActive);

        if (domain is null)
            return null;

        var settings = await dbContext.StorefrontSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == domain.TenantId);

        if (settings is null)
            return null;

        var info = new StorefrontTenantInfo(domain.TenantId, settings, domain);

        cache.Set(key, info, TimeSpan.FromMinutes(10));

        return info;
    }

    public void InvalidateCache(string hostname)
    {
        cache.Remove(CacheKey(hostname));
    }
}

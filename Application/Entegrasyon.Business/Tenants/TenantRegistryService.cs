using Microsoft.Extensions.Caching.Memory;

namespace Entegrasyon.Business.Tenants;

public sealed class TenantRegistryService(
    ITenantRegistryDataSource dataSource,
    IMemoryCache cache) : ITenantRegistry
{
    private const string CacheKey = "tenant:registry:all";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<TenantRegistryEntry?> GetBySubdomainAsync(string subdomain)
    {
        var tenants = await GetCachedTenantsAsync();
        return tenants.FirstOrDefault(t =>
            string.Equals(t.Subdomain, subdomain, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<TenantRegistryEntry?> GetByIdAsync(int tenantId)
    {
        var tenants = await GetCachedTenantsAsync();
        return tenants.FirstOrDefault(t => t.TenantId == tenantId);
    }

    public async Task<IReadOnlyList<TenantRegistryEntry>> GetAllActiveAsync()
    {
        var tenants = await GetCachedTenantsAsync();
        return tenants.Where(t => t.IsActive).ToList().AsReadOnly();
    }

    public void InvalidateCache()
    {
        cache.Remove(CacheKey);
    }

    private async Task<IReadOnlyList<TenantRegistryEntry>> GetCachedTenantsAsync()
    {
        if (cache.TryGetValue(CacheKey, out IReadOnlyList<TenantRegistryEntry>? cached) && cached is not null)
            return cached;

        var tenants = await dataSource.GetAllTenantsAsync();
        cache.Set(CacheKey, tenants, CacheDuration);
        return tenants;
    }
}

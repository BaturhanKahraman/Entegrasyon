namespace Entegrasyon.Business.Tenants;

/// <summary>
/// Tenant kayit defteri — singleton, tum aktif tenant'lari cache'ler.
/// Background service'ler ve middleware tarafindan kullanilir.
/// </summary>
public interface ITenantRegistry
{
    Task<TenantRegistryEntry?> GetBySubdomainAsync(string subdomain);
    Task<TenantRegistryEntry?> GetByIdAsync(int tenantId);
    Task<IReadOnlyList<TenantRegistryEntry>> GetAllActiveAsync();
    void InvalidateCache();
}

/// <summary>
/// Tenant verisini AdminPanel DB'den okuyan data source.
/// ITenantRegistry bunu kullanarak cache doldurur.
/// Test'te mocklanir.
/// </summary>
public interface ITenantRegistryDataSource
{
    Task<IReadOnlyList<TenantRegistryEntry>> GetAllTenantsAsync();
}

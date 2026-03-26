using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Tenants;

namespace Entegrasyon.Business.Concrete;

/// <summary>
/// Scoped tenant context — middleware veya background service tarafindan initialize edilir.
/// StorefrontTenantContext pattern'ini takip eder.
/// </summary>
public sealed class HttpTenantContext : ITenantContext
{
    private TenantRegistryEntry? _entry;

    public int TenantId => _entry?.TenantId
        ?? throw new InvalidOperationException("Tenant context is not initialized.");

    public string ConnectionString => _entry?.ConnectionString
        ?? throw new InvalidOperationException("Tenant context is not initialized.");

    public bool IsInitialized => _entry is not null;

    public void Initialize(TenantRegistryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _entry = entry;
    }

    public int GetMarketPlaceId(string marketplaceName)
    {
        throw new InvalidOperationException(
            "Multi-tenant modda GetMarketPlaceId() yerine scoped IMarketPlaceManager kullanin.");
    }
}

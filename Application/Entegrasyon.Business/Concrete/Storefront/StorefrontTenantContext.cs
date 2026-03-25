using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.Business.Concrete.Storefront;

public class StorefrontTenantContext : IStorefrontTenantContext
{
    private StorefrontTenantInfo? _info;

    public int TenantId => _info?.TenantId
        ?? throw new InvalidOperationException("Tenant context is not initialized.");

    public StorefrontSettings Settings => _info?.Settings
        ?? throw new InvalidOperationException("Tenant context is not initialized.");

    public StorefrontDomainMapping Domain => _info?.Domain
        ?? throw new InvalidOperationException("Tenant context is not initialized.");

    public bool IsInitialized => _info is not null;

    public void Initialize(StorefrontTenantInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);
        _info = info;
    }
}

using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.Business.Abstract;

public record StorefrontTenantInfo(
    int TenantId,
    StorefrontSettings Settings,
    StorefrontDomainMapping Domain);

public interface IStorefrontTenantContext
{
    int TenantId { get; }
    StorefrontSettings Settings { get; }
    StorefrontDomainMapping Domain { get; }
    bool IsInitialized { get; }
    void Initialize(StorefrontTenantInfo info);
}

namespace Entegrasyon.Business.Tenants;

/// <summary>
/// AdminPanel DB'den okunan tenant bilgisinin inmemory temsili.
/// Singleton ITenantRegistry tarafindan cache'lenir.
/// </summary>
public record TenantRegistryEntry(
    int TenantId,
    string Subdomain,
    string CompanyName,
    string ConnectionString,
    bool IsActive,
    string? LicenseType);

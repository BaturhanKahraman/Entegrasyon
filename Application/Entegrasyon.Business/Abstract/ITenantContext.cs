using Entegrasyon.Business.Tenants;

namespace Entegrasyon.Business.Abstract;

/// <summary>
/// Mevcut tenant baglamini saglar.
/// HTTP request'ten subdomain uzerinden cozumlenir.
/// Background service'lerde manuel olarak initialize edilir.
/// </summary>
public interface ITenantContext
{
    /// <summary>Mevcut tenant ID.</summary>
    int TenantId { get; }

    /// <summary>Tenant'in veritabani connection string'i.</summary>
    string ConnectionString { get; }

    /// <summary>Tenant context basariyla initialize edildi mi?</summary>
    bool IsInitialized { get; }

    /// <summary>Tenant bilgisini set eder. Request basina bir kez cagirilir.</summary>
    void Initialize(TenantRegistryEntry entry);

    /// <summary>
    /// Belirtilen marketplace turu icin bu tenant'in marketplace ID'sini doner.
    /// </summary>
    int GetMarketPlaceId(string marketplaceName);
}

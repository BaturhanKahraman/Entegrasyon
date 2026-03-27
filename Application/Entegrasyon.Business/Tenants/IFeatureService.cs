namespace Entegrasyon.Business.Tenants;

/// <summary>
/// Tenant'in aktif paketindeki permission'lari kontrol eder.
/// Iki katmanli yetkilendirmenin ilk katmani: tenant seviyesi feature kontrolu.
/// </summary>
public interface IFeatureService
{
    /// <summary>
    /// Bu tenant'in aktif paketi bu permission'i iceriyor mu?
    /// </summary>
    Task<bool> IsFeatureEnabledAsync(string permissionKey);

    /// <summary>
    /// Bu tenant'in aktif paketindeki tum permission'lari doner.
    /// </summary>
    Task<IReadOnlySet<string>> GetEnabledFeaturesAsync();
}

/// <summary>
/// Tenant'in feature listesini AdminPanel DB'den okur.
/// Test'te mocklanir.
/// </summary>
public interface IFeatureDataSource
{
    /// <summary>
    /// Belirtilen tenant'in aktif subscription'indaki paket permission'larini doner.
    /// </summary>
    Task<IReadOnlySet<string>> GetTenantFeaturesAsync(int tenantId);
}

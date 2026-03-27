using Entegrasyon.Business.Abstract;

namespace Entegrasyon.Business.Tenants;

/// <summary>
/// Scoped servis — ITenantContext'ten tenant ID alir,
/// IFeatureDataSource'tan o tenant'in permission set'ini cekar.
/// </summary>
public sealed class FeatureService(
    ITenantContext tenantContext,
    IFeatureDataSource dataSource) : IFeatureService
{
    private IReadOnlySet<string>? _cachedFeatures;

    public async Task<bool> IsFeatureEnabledAsync(string permissionKey)
    {
        if (!tenantContext.IsInitialized)
            return false;

        var features = await GetEnabledFeaturesAsync();
        return features.Contains(permissionKey);
    }

    public async Task<IReadOnlySet<string>> GetEnabledFeaturesAsync()
    {
        if (!tenantContext.IsInitialized)
            return new HashSet<string>();

        if (_cachedFeatures is not null)
            return _cachedFeatures;

        _cachedFeatures = await dataSource.GetTenantFeaturesAsync(tenantContext.TenantId);
        return _cachedFeatures;
    }
}

using Entegrasyon.Business.Abstract;
using Microsoft.Extensions.Caching.Memory;

namespace Entegrasyon.Business.Tenants;

/// <summary>
/// IMemoryCache wrapper — cache key'leri t:{tenantId}: prefix'i ile izole eder.
/// Scoped servis olarak register edilir. Manager'lar bunu inject eder.
/// </summary>
public sealed class TenantMemoryCache(IMemoryCache inner, ITenantContext tenantContext)
{
    private string Key(string key) => $"t:{tenantContext.TenantId}:{key}";

    public bool TryGetValue<T>(string key, out T? value)
    {
        return inner.TryGetValue(Key(key), out value);
    }

    public T Set<T>(string key, T value, TimeSpan expiration)
    {
        return inner.Set(Key(key), value, expiration);
    }

    public void Remove(string key)
    {
        inner.Remove(Key(key));
    }
}

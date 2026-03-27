using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Tenants;
using Microsoft.Extensions.Caching.Memory;

namespace Entegrasyon.UnitTest.Tenants;

public class TenantMemoryCacheTests
{
    [Fact]
    public void Set_And_TryGetValue_UsesTenantPrefixedKey()
    {
        var innerCache = new MemoryCache(new MemoryCacheOptions());
        var tenantContext = new HttpTenantContext();
        tenantContext.Initialize(new TenantRegistryEntry(5, "acme", "Acme", "conn", true, null));

        var tenantCache = new TenantMemoryCache(innerCache, tenantContext);

        tenantCache.Set("brands:list", new[] { "Brand1" }, TimeSpan.FromMinutes(5));

        tenantCache.TryGetValue<string[]>("brands:list", out var result).Should().BeTrue();
        result.Should().Contain("Brand1");

        // Verify the inner cache has the prefixed key
        innerCache.TryGetValue("t:5:brands:list", out _).Should().BeTrue();
        // Verify unprefixed key does NOT exist
        innerCache.TryGetValue("brands:list", out _).Should().BeFalse();
    }

    [Fact]
    public void DifferentTenants_HaveIsolatedCaches()
    {
        var innerCache = new MemoryCache(new MemoryCacheOptions());

        var ctx1 = new HttpTenantContext();
        ctx1.Initialize(new TenantRegistryEntry(1, "t1", "T1", "c1", true, null));
        var cache1 = new TenantMemoryCache(innerCache, ctx1);

        var ctx2 = new HttpTenantContext();
        ctx2.Initialize(new TenantRegistryEntry(2, "t2", "T2", "c2", true, null));
        var cache2 = new TenantMemoryCache(innerCache, ctx2);

        cache1.Set("key", "tenant1-value", TimeSpan.FromMinutes(5));
        cache2.Set("key", "tenant2-value", TimeSpan.FromMinutes(5));

        cache1.TryGetValue<string>("key", out var v1).Should().BeTrue();
        v1.Should().Be("tenant1-value");

        cache2.TryGetValue<string>("key", out var v2).Should().BeTrue();
        v2.Should().Be("tenant2-value");
    }

    [Fact]
    public void Remove_RemovesTenantPrefixedKey()
    {
        var innerCache = new MemoryCache(new MemoryCacheOptions());
        var tenantContext = new HttpTenantContext();
        tenantContext.Initialize(new TenantRegistryEntry(1, "t", "T", "c", true, null));

        var tenantCache = new TenantMemoryCache(innerCache, tenantContext);
        tenantCache.Set("key", "value", TimeSpan.FromMinutes(5));
        tenantCache.Remove("key");

        tenantCache.TryGetValue<string>("key", out _).Should().BeFalse();
    }
}

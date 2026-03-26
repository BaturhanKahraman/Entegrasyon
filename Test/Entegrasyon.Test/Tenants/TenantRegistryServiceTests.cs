using Entegrasyon.Business.Tenants;
using Microsoft.Extensions.Caching.Memory;

namespace Entegrasyon.UnitTest.Tenants;

public class TenantRegistryServiceTests
{
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    [Fact]
    public async Task GetBySubdomainAsync_WhenTenantExists_ReturnsTenant()
    {
        var mockDataSource = new Mock<ITenantRegistryDataSource>();
        mockDataSource.Setup(x => x.GetAllTenantsAsync())
            .ReturnsAsync(new List<TenantRegistryEntry>
            {
                new(1, "acme", "Acme Corp", "Host=localhost;Database=tenant_1", true, "Standard"),
                new(2, "beta", "Beta Inc", "Host=localhost;Database=tenant_2", true, "Pro"),
            });

        var service = new TenantRegistryService(mockDataSource.Object, _cache);
        var result = await service.GetBySubdomainAsync("acme");

        result.Should().NotBeNull();
        result!.TenantId.Should().Be(1);
        result.CompanyName.Should().Be("Acme Corp");
    }

    [Fact]
    public async Task GetBySubdomainAsync_WhenNotFound_ReturnsNull()
    {
        var mockDataSource = new Mock<ITenantRegistryDataSource>();
        mockDataSource.Setup(x => x.GetAllTenantsAsync())
            .ReturnsAsync(new List<TenantRegistryEntry>());

        var service = new TenantRegistryService(mockDataSource.Object, _cache);
        var result = await service.GetBySubdomainAsync("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenTenantExists_ReturnsTenant()
    {
        var mockDataSource = new Mock<ITenantRegistryDataSource>();
        mockDataSource.Setup(x => x.GetAllTenantsAsync())
            .ReturnsAsync(new List<TenantRegistryEntry>
            {
                new(3, "gamma", "Gamma LLC", "Host=localhost;Database=tenant_3", true, "Enterprise"),
            });

        var service = new TenantRegistryService(mockDataSource.Object, _cache);
        var result = await service.GetByIdAsync(3);

        result.Should().NotBeNull();
        result!.Subdomain.Should().Be("gamma");
    }

    [Fact]
    public async Task GetAllActiveAsync_ReturnsOnlyActiveTenants()
    {
        var mockDataSource = new Mock<ITenantRegistryDataSource>();
        mockDataSource.Setup(x => x.GetAllTenantsAsync())
            .ReturnsAsync(new List<TenantRegistryEntry>
            {
                new(1, "acme", "Acme", "conn1", true, "Standard"),
                new(2, "beta", "Beta", "conn2", false, "Standard"),
                new(3, "gamma", "Gamma", "conn3", true, "Pro"),
            });

        var service = new TenantRegistryService(mockDataSource.Object, _cache);
        var result = await service.GetAllActiveAsync();

        result.Should().HaveCount(2);
        result.Select(t => t.Subdomain).Should().Contain("acme").And.Contain("gamma");
    }

    [Fact]
    public async Task GetBySubdomainAsync_UsesCacheOnSecondCall()
    {
        var mockDataSource = new Mock<ITenantRegistryDataSource>();
        mockDataSource.Setup(x => x.GetAllTenantsAsync())
            .ReturnsAsync(new List<TenantRegistryEntry>
            {
                new(1, "acme", "Acme", "conn1", true, "Standard"),
            });

        var service = new TenantRegistryService(mockDataSource.Object, _cache);

        await service.GetBySubdomainAsync("acme");
        await service.GetBySubdomainAsync("acme");

        mockDataSource.Verify(x => x.GetAllTenantsAsync(), Times.Once);
    }

    [Fact]
    public async Task InvalidateCache_ClearsCache()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var mockDataSource = new Mock<ITenantRegistryDataSource>();
        mockDataSource.Setup(x => x.GetAllTenantsAsync())
            .ReturnsAsync(new List<TenantRegistryEntry>
            {
                new(1, "acme", "Acme", "conn1", true, "Standard"),
            });

        var service = new TenantRegistryService(mockDataSource.Object, cache);

        await service.GetBySubdomainAsync("acme");
        await service.GetBySubdomainAsync("acme");
        mockDataSource.Verify(x => x.GetAllTenantsAsync(), Times.Once);

        service.InvalidateCache();
        await service.GetBySubdomainAsync("acme");
        mockDataSource.Verify(x => x.GetAllTenantsAsync(), Times.Exactly(2));
    }
}

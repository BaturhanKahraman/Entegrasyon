using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Tenants;

namespace Entegrasyon.UnitTest.Tenants;

public class FeatureServiceTests
{
    [Fact]
    public async Task IsFeatureEnabledAsync_WhenTenantHasPackageWithPermission_ReturnsTrue()
    {
        var tenantContext = new HttpTenantContext();
        tenantContext.Initialize(new TenantRegistryEntry(1, "acme", "Acme", "conn", true, "Standard"));

        var mockDataSource = new Mock<IFeatureDataSource>();
        mockDataSource.Setup(x => x.GetTenantFeaturesAsync(1))
            .ReturnsAsync(new HashSet<string>
            {
                "Permissions.Products.View",
                "Permissions.Products.Create",
                "Permissions.Categories.View"
            });

        var service = new FeatureService(tenantContext, mockDataSource.Object);

        var result = await service.IsFeatureEnabledAsync("Permissions.Products.View");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsFeatureEnabledAsync_WhenTenantHasNoSubscription_ReturnsFalse()
    {
        var tenantContext = new HttpTenantContext();
        tenantContext.Initialize(new TenantRegistryEntry(1, "acme", "Acme", "conn", true, null));

        var mockDataSource = new Mock<IFeatureDataSource>();
        mockDataSource.Setup(x => x.GetTenantFeaturesAsync(1))
            .ReturnsAsync(new HashSet<string>());

        var service = new FeatureService(tenantContext, mockDataSource.Object);

        var result = await service.IsFeatureEnabledAsync("Permissions.Products.View");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsFeatureEnabledAsync_WhenPermissionNotInPackage_ReturnsFalse()
    {
        var tenantContext = new HttpTenantContext();
        tenantContext.Initialize(new TenantRegistryEntry(1, "acme", "Acme", "conn", true, "Standard"));

        var mockDataSource = new Mock<IFeatureDataSource>();
        mockDataSource.Setup(x => x.GetTenantFeaturesAsync(1))
            .ReturnsAsync(new HashSet<string>
            {
                "Permissions.Products.View",
            });

        var service = new FeatureService(tenantContext, mockDataSource.Object);

        var result = await service.IsFeatureEnabledAsync("Permissions.Marketplace.View");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetEnabledFeaturesAsync_ReturnsAllPermissionsFromActiveSubscription()
    {
        var tenantContext = new HttpTenantContext();
        tenantContext.Initialize(new TenantRegistryEntry(1, "acme", "Acme", "conn", true, "Standard"));

        var permissions = new HashSet<string>
        {
            "Permissions.Products.View",
            "Permissions.Products.Create",
            "Permissions.Categories.View"
        };

        var mockDataSource = new Mock<IFeatureDataSource>();
        mockDataSource.Setup(x => x.GetTenantFeaturesAsync(1))
            .ReturnsAsync(permissions);

        var service = new FeatureService(tenantContext, mockDataSource.Object);

        var result = await service.GetEnabledFeaturesAsync();

        result.Should().HaveCount(3);
        result.Should().Contain("Permissions.Products.View");
        result.Should().Contain("Permissions.Categories.View");
    }

    [Fact]
    public async Task IsFeatureEnabledAsync_WhenTenantNotInitialized_ReturnsFalse()
    {
        var tenantContext = new HttpTenantContext();
        // NOT initialized

        var mockDataSource = new Mock<IFeatureDataSource>();

        var service = new FeatureService(tenantContext, mockDataSource.Object);

        var result = await service.IsFeatureEnabledAsync("Permissions.Products.View");

        result.Should().BeFalse();
    }
}

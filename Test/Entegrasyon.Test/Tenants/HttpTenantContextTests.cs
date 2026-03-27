using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Tenants;

namespace Entegrasyon.UnitTest.Tenants;

public class HttpTenantContextTests
{
    [Fact]
    public void TenantId_WhenNotInitialized_ThrowsInvalidOperationException()
    {
        var context = new HttpTenantContext();
        var act = () => context.TenantId;
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not initialized*");
    }

    [Fact]
    public void ConnectionString_WhenNotInitialized_ThrowsInvalidOperationException()
    {
        var context = new HttpTenantContext();
        var act = () => context.ConnectionString;
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not initialized*");
    }

    [Fact]
    public void IsInitialized_WhenNotInitialized_ReturnsFalse()
    {
        var context = new HttpTenantContext();
        context.IsInitialized.Should().BeFalse();
    }

    [Fact]
    public void Initialize_SetsTenantIdAndConnectionString()
    {
        var context = new HttpTenantContext();
        var entry = new TenantRegistryEntry(
            TenantId: 5,
            Subdomain: "acme",
            CompanyName: "Acme Corp",
            ConnectionString: "Host=localhost;Database=tenant_5",
            IsActive: true,
            LicenseType: "Standard");

        context.Initialize(entry);

        context.TenantId.Should().Be(5);
        context.ConnectionString.Should().Be("Host=localhost;Database=tenant_5");
        context.IsInitialized.Should().BeTrue();
    }

    [Fact]
    public void Initialize_WithNull_ThrowsArgumentNullException()
    {
        var context = new HttpTenantContext();
        var act = () => context.Initialize(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetMarketPlaceId_ThrowsInvalidOperationException()
    {
        var context = new HttpTenantContext();
        var entry = new TenantRegistryEntry(1, "test", "Test", "conn", true, null);
        context.Initialize(entry);

        var act = () => context.GetMarketPlaceId("Trendyol");
        act.Should().Throw<InvalidOperationException>();
    }
}

using Entegrasyon.Business.Concrete.Storefront;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.UnitTest.Storefront;

public class StorefrontSettingsManagerTests
{
    private readonly Mock<IDbContextFactory<IntegrationDbContext>> _mockContextFactory;
    private readonly Mock<IntegrationDbContext> _mockDbContext;
    private readonly StorefrontSettingsManager _sut;

    public StorefrontSettingsManagerTests()
    {
        _mockDbContext = new Mock<IntegrationDbContext>(
            new DbContextOptionsBuilder<IntegrationDbContext>().Options);
        _mockContextFactory = new Mock<IDbContextFactory<IntegrationDbContext>>();
        _mockContextFactory
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockDbContext.Object);

        _sut = new StorefrontSettingsManager(_mockContextFactory.Object);
    }

    [Fact]
    public async Task GetByTenantIdAsync_ExistingTenant_ReturnsSuccess()
    {
        // Arrange
        var settings = new StorefrontSettings
        {
            Id = 1, TenantId = 10, StoreName = "Test Store",
            CompanyName = "Test Co", CompanyTaxOffice = "Istanbul",
            CompanyTaxNumber = "1234567890", ContactPhone = "555-0100",
            ContactEmail = "info@test.com", Address = "Test Address", City = "Istanbul"
        };

        _mockDbContext.Setup(x => x.StorefrontSettings)
            .ReturnsDbSet(new List<StorefrontSettings> { settings });

        // Act
        var result = await _sut.GetByTenantIdAsync(10);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.StoreName.Should().Be("Test Store");
    }

    [Fact]
    public async Task GetByTenantIdAsync_NonexistentTenant_ReturnsError()
    {
        // Arrange
        _mockDbContext.Setup(x => x.StorefrontSettings)
            .ReturnsDbSet(new List<StorefrontSettings>());

        // Act
        var result = await _sut.GetByTenantIdAsync(999);

        // Assert
        result.Success.Should().BeFalse();
    }
}

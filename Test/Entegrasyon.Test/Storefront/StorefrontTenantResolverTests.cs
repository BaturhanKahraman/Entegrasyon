using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Storefront;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.UnitTest.Storefront;

public class StorefrontTenantResolverTests
{
    private readonly Mock<IDbContextFactory<IntegrationDbContext>> _mockContextFactory;
    private readonly Mock<IntegrationDbContext> _mockDbContext;
    private readonly MemoryCache _cache;
    private readonly StorefrontTenantResolver _sut;

    public StorefrontTenantResolverTests()
    {
        _mockDbContext = new Mock<IntegrationDbContext>(
            new DbContextOptionsBuilder<IntegrationDbContext>().Options);
        _mockContextFactory = new Mock<IDbContextFactory<IntegrationDbContext>>();
        _mockContextFactory
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockDbContext.Object);
        _cache = new MemoryCache(new MemoryCacheOptions());

        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider
            .Setup(sp => sp.GetService(typeof(IDbContextFactory<IntegrationDbContext>)))
            .Returns(_mockContextFactory.Object);
        var mockScope = new Mock<IServiceScope>();
        mockScope.Setup(s => s.ServiceProvider).Returns(mockServiceProvider.Object);
        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        mockScopeFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object);

        _sut = new StorefrontTenantResolver(mockScopeFactory.Object, _cache);
    }

    [Fact]
    public async Task ResolveAsync_KnownDomain_ReturnsTenantInfo()
    {
        // Arrange
        var domain = new StorefrontDomainMapping
        {
            Id = 1, TenantId = 10, DomainName = "shop.example.com",
            IsPrimary = true, IsActive = true
        };
        var settings = new StorefrontSettings
        {
            Id = 1, TenantId = 10, StoreName = "Test Store",
            CompanyName = "Test Co", CompanyTaxOffice = "Istanbul",
            CompanyTaxNumber = "1234567890", ContactPhone = "555-0100",
            ContactEmail = "info@test.com", Address = "Test Address", City = "Istanbul"
        };

        _mockDbContext.Setup(x => x.StorefrontDomainMappings)
            .ReturnsDbSet(new List<StorefrontDomainMapping> { domain });
        _mockDbContext.Setup(x => x.StorefrontSettings)
            .ReturnsDbSet(new List<StorefrontSettings> { settings });

        // Act
        var result = await _sut.ResolveAsync("shop.example.com");

        // Assert
        result.Should().NotBeNull();
        result!.TenantId.Should().Be(10);
        result.Settings.StoreName.Should().Be("Test Store");
        result.Domain.DomainName.Should().Be("shop.example.com");
    }

    [Fact]
    public async Task ResolveAsync_UnknownDomain_ReturnsNull()
    {
        // Arrange
        _mockDbContext.Setup(x => x.StorefrontDomainMappings)
            .ReturnsDbSet(new List<StorefrontDomainMapping>());
        _mockDbContext.Setup(x => x.StorefrontSettings)
            .ReturnsDbSet(new List<StorefrontSettings>());

        // Act
        var result = await _sut.ResolveAsync("unknown.example.com");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_InactiveDomain_ReturnsNull()
    {
        // Arrange
        var domain = new StorefrontDomainMapping
        {
            Id = 1, TenantId = 10, DomainName = "inactive.example.com",
            IsPrimary = true, IsActive = false
        };

        _mockDbContext.Setup(x => x.StorefrontDomainMappings)
            .ReturnsDbSet(new List<StorefrontDomainMapping> { domain });
        _mockDbContext.Setup(x => x.StorefrontSettings)
            .ReturnsDbSet(new List<StorefrontSettings>());

        // Act
        var result = await _sut.ResolveAsync("inactive.example.com");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_CachesResult_OnSecondCall()
    {
        // Arrange
        var domain = new StorefrontDomainMapping
        {
            Id = 1, TenantId = 10, DomainName = "cached.example.com",
            IsPrimary = true, IsActive = true
        };
        var settings = new StorefrontSettings
        {
            Id = 1, TenantId = 10, StoreName = "Cached Store",
            CompanyName = "Test Co", CompanyTaxOffice = "Istanbul",
            CompanyTaxNumber = "1234567890", ContactPhone = "555-0100",
            ContactEmail = "info@test.com", Address = "Test Address", City = "Istanbul"
        };

        _mockDbContext.Setup(x => x.StorefrontDomainMappings)
            .ReturnsDbSet(new List<StorefrontDomainMapping> { domain });
        _mockDbContext.Setup(x => x.StorefrontSettings)
            .ReturnsDbSet(new List<StorefrontSettings> { settings });

        // Act
        await _sut.ResolveAsync("cached.example.com");
        await _sut.ResolveAsync("cached.example.com");

        // Assert — DB context should be created only once (cached on second call)
        _mockContextFactory.Verify(
            f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InvalidateCache_ClearsEntry()
    {
        // Arrange
        var domain = new StorefrontDomainMapping
        {
            Id = 1, TenantId = 10, DomainName = "invalidate.example.com",
            IsPrimary = true, IsActive = true
        };
        var settings = new StorefrontSettings
        {
            Id = 1, TenantId = 10, StoreName = "Invalidate Store",
            CompanyName = "Test Co", CompanyTaxOffice = "Istanbul",
            CompanyTaxNumber = "1234567890", ContactPhone = "555-0100",
            ContactEmail = "info@test.com", Address = "Test Address", City = "Istanbul"
        };

        _mockDbContext.Setup(x => x.StorefrontDomainMappings)
            .ReturnsDbSet(new List<StorefrontDomainMapping> { domain });
        _mockDbContext.Setup(x => x.StorefrontSettings)
            .ReturnsDbSet(new List<StorefrontSettings> { settings });

        // Act
        await _sut.ResolveAsync("invalidate.example.com");
        _sut.InvalidateCache("invalidate.example.com");
        await _sut.ResolveAsync("invalidate.example.com");

        // Assert — DB context should be created twice (cache was cleared)
        _mockContextFactory.Verify(
            f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }
}

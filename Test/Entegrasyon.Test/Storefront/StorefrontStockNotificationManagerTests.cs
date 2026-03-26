using Entegrasyon.Business.Concrete.Storefront;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.UnitTest.Storefront;

public class StorefrontStockNotificationManagerTests
{
    private readonly Mock<IDbContextFactory<IntegrationDbContext>> _mockContextFactory;
    private readonly Mock<IntegrationDbContext> _mockDbContext;
    private readonly StorefrontStockNotificationManager _sut;

    public StorefrontStockNotificationManagerTests()
    {
        _mockDbContext = new Mock<IntegrationDbContext>(
            new DbContextOptionsBuilder<IntegrationDbContext>().Options);
        _mockContextFactory = new Mock<IDbContextFactory<IntegrationDbContext>>();
        _mockContextFactory
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockDbContext.Object);

        _sut = new StorefrontStockNotificationManager(_mockContextFactory.Object);
    }

    [Fact]
    public async Task SubscribeAsync_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        _mockDbContext.Setup(x => x.StorefrontStockNotifications)
            .ReturnsDbSet(new List<StorefrontStockNotification>());
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _sut.SubscribeAsync(1, Guid.NewGuid(), "test@example.com");

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("basariyla");
    }

    [Fact]
    public async Task SubscribeAsync_DuplicateNotNotified_ReturnsError()
    {
        // Arrange
        var variantId = Guid.NewGuid();
        var existing = new List<StorefrontStockNotification>
        {
            new() { Id = 1, TenantId = 1, ProductVariantId = variantId, Email = "test@example.com", IsNotified = false }
        };

        _mockDbContext.Setup(x => x.StorefrontStockNotifications).ReturnsDbSet(existing);

        // Act
        var result = await _sut.SubscribeAsync(1, variantId, "test@example.com");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("zaten");
    }

    [Fact]
    public async Task SubscribeAsync_PreviouslyNotified_ReactivatesSubscription()
    {
        // Arrange
        var variantId = Guid.NewGuid();
        var existing = new List<StorefrontStockNotification>
        {
            new() { Id = 1, TenantId = 1, ProductVariantId = variantId, Email = "test@example.com", IsNotified = true, NotifiedAt = DateTimeOffset.UtcNow }
        };

        _mockDbContext.Setup(x => x.StorefrontStockNotifications).ReturnsDbSet(existing);
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _sut.SubscribeAsync(1, variantId, "test@example.com");

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("yeniden");
    }

    [Fact]
    public async Task SubscribeAsync_EmptyEmail_ReturnsError()
    {
        // Act
        var result = await _sut.SubscribeAsync(1, Guid.NewGuid(), "");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("E-posta");
    }

    [Fact]
    public async Task SubscribeAsync_EmptyVariantId_ReturnsError()
    {
        // Act
        var result = await _sut.SubscribeAsync(1, Guid.Empty, "test@example.com");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("varyanti");
    }

    [Fact]
    public async Task GetSubscriberCountAsync_ReturnsActiveCount()
    {
        // Arrange
        var variantId = Guid.NewGuid();
        var notifications = new List<StorefrontStockNotification>
        {
            new() { Id = 1, TenantId = 1, ProductVariantId = variantId, Email = "a@test.com", IsNotified = false },
            new() { Id = 2, TenantId = 1, ProductVariantId = variantId, Email = "b@test.com", IsNotified = false },
            new() { Id = 3, TenantId = 1, ProductVariantId = variantId, Email = "c@test.com", IsNotified = true },
        };

        _mockDbContext.Setup(x => x.StorefrontStockNotifications).ReturnsDbSet(notifications);

        // Act
        var result = await _sut.GetSubscriberCountAsync(1, variantId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().Be(2);
    }
}

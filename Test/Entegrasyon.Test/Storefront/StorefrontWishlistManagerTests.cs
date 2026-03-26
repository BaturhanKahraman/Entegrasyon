using Entegrasyon.Business.Concrete.Storefront;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.UnitTest.Storefront;

public class StorefrontWishlistManagerTests
{
    private readonly Mock<IDbContextFactory<IntegrationDbContext>> _mockContextFactory;
    private readonly Mock<IntegrationDbContext> _mockDbContext;
    private readonly StorefrontWishlistManager _sut;

    public StorefrontWishlistManagerTests()
    {
        _mockDbContext = new Mock<IntegrationDbContext>(
            new DbContextOptionsBuilder<IntegrationDbContext>().Options);
        _mockContextFactory = new Mock<IDbContextFactory<IntegrationDbContext>>();
        _mockContextFactory
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockDbContext.Object);

        _sut = new StorefrontWishlistManager(_mockContextFactory.Object);
    }

    [Fact]
    public async Task AddToWishlistAsync_DuplicateItem_ReturnsError()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var items = new List<StorefrontWishlistItem>
        {
            new() { Id = 1, TenantId = 1, CustomerId = 42, ProductId = productId }
        };

        _mockDbContext.Setup(x => x.StorefrontWishlistItems).ReturnsDbSet(items);

        // Act
        var result = await _sut.AddToWishlistAsync(1, 42, productId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("zaten");
    }

    [Fact]
    public async Task RemoveFromWishlistAsync_NonexistentItem_ReturnsError()
    {
        // Arrange
        _mockDbContext.Setup(x => x.StorefrontWishlistItems).ReturnsDbSet(new List<StorefrontWishlistItem>());

        // Act
        var result = await _sut.RemoveFromWishlistAsync(1, 42, Guid.NewGuid());

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("bulunamadi");
    }

    [Fact]
    public async Task IsInWishlistAsync_ExistingItem_ReturnsTrue()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var items = new List<StorefrontWishlistItem>
        {
            new() { Id = 1, TenantId = 1, CustomerId = 42, ProductId = productId }
        };

        _mockDbContext.Setup(x => x.StorefrontWishlistItems).ReturnsDbSet(items);

        // Act
        var result = await _sut.IsInWishlistAsync(1, 42, productId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsInWishlistAsync_NonexistentItem_ReturnsFalse()
    {
        // Arrange
        _mockDbContext.Setup(x => x.StorefrontWishlistItems).ReturnsDbSet(new List<StorefrontWishlistItem>());

        // Act
        var result = await _sut.IsInWishlistAsync(1, 42, Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }
}

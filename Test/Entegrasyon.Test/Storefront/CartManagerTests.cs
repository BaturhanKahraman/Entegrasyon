using Entegrasyon.Business.Concrete.Storefront;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.UnitTest.Storefront;

public class CartManagerTests
{
    private readonly Mock<IDbContextFactory<IntegrationDbContext>> _mockContextFactory;
    private readonly Mock<IntegrationDbContext> _mockDbContext;
    private readonly CartManager _sut;

    public CartManagerTests()
    {
        _mockDbContext = new Mock<IntegrationDbContext>(
            new DbContextOptionsBuilder<IntegrationDbContext>().Options);
        _mockContextFactory = new Mock<IDbContextFactory<IntegrationDbContext>>();
        _mockContextFactory
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockDbContext.Object);

        _sut = new CartManager(_mockContextFactory.Object);
    }

    [Fact]
    public async Task GetOrCreateCartAsync_NoExisting_CreatesNew()
    {
        // Arrange
        _mockDbContext.Setup(x => x.Carts).ReturnsDbSet(new List<Cart>());
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.GetOrCreateCartAsync(tenantId: 1, customerId: 5, sessionId: null);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.TenantId.Should().Be(1);
        result.Data.CustomerId.Should().Be(5);
        result.Data.ExpiresAt.Should().BeCloseTo(
            DateTimeOffset.UtcNow.AddDays(7), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetOrCreateCartAsync_ExistingCart_ReturnsExisting()
    {
        // Arrange
        var existingCart = new Cart
        {
            Id = Guid.NewGuid(), TenantId = 1, CustomerId = 5,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(5)
        };

        _mockDbContext.Setup(x => x.Carts).ReturnsDbSet(new List<Cart> { existingCart });

        // Act
        var result = await _sut.GetOrCreateCartAsync(tenantId: 1, customerId: 5, sessionId: null);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Id.Should().Be(existingCart.Id);
    }

    [Fact]
    public async Task AddToCartAsync_NewItem_AddsWithCorrectPrice()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var cart = new Cart
        {
            Id = cartId, TenantId = 1,
            Items = new List<CartItem>(),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };
        var variant = new ProductVariant
        {
            Id = variantId, SalePrice = 99.90m,
            BranchOfficeStocks = new List<BranchOfficeStock>
            {
                new() { BranchOfficeId = 1, ProductVariantId = variantId, CurrentStock = 10 }
            }
        };

        _mockDbContext.Setup(x => x.Carts).ReturnsDbSet(new List<Cart> { cart });
        _mockDbContext.Setup(x => x.ProductVariants).ReturnsDbSet(new List<ProductVariant> { variant });
        _mockDbContext.Setup(x => x.CartItems).ReturnsDbSet(new List<CartItem>());
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.AddToCartAsync(cartId, variantId, quantity: 2);

        // Assert
        result.Success.Should().BeTrue();
        cart.Items.Should().HaveCount(1);
        cart.Items.First().UnitPrice.Should().Be(99.90m);
        cart.Items.First().Quantity.Should().Be(2);
    }

    [Fact]
    public async Task AddToCartAsync_ExistingItem_IncreasesQuantity()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var existingItem = new CartItem
        {
            Id = 1, CartId = cartId, ProductVariantId = variantId,
            Quantity = 2, UnitPrice = 50m, AddedAt = DateTimeOffset.UtcNow
        };
        var cart = new Cart
        {
            Id = cartId, TenantId = 1,
            Items = new List<CartItem> { existingItem },
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };
        var variant = new ProductVariant
        {
            Id = variantId, SalePrice = 50m,
            BranchOfficeStocks = new List<BranchOfficeStock>
            {
                new() { BranchOfficeId = 1, ProductVariantId = variantId, CurrentStock = 10 }
            }
        };

        _mockDbContext.Setup(x => x.Carts).ReturnsDbSet(new List<Cart> { cart });
        _mockDbContext.Setup(x => x.ProductVariants).ReturnsDbSet(new List<ProductVariant> { variant });
        _mockDbContext.Setup(x => x.CartItems).ReturnsDbSet(new List<CartItem> { existingItem });
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.AddToCartAsync(cartId, variantId, quantity: 3);

        // Assert
        result.Success.Should().BeTrue();
        existingItem.Quantity.Should().Be(5); // 2 + 3
    }

    [Fact]
    public async Task AddToCartAsync_InsufficientStock_ReturnsError()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var cart = new Cart
        {
            Id = cartId, TenantId = 1,
            Items = new List<CartItem>(),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };
        var variant = new ProductVariant
        {
            Id = variantId, SalePrice = 50m,
            BranchOfficeStocks = new List<BranchOfficeStock>
            {
                new() { BranchOfficeId = 1, ProductVariantId = variantId, CurrentStock = 1 }
            }
        };

        _mockDbContext.Setup(x => x.Carts).ReturnsDbSet(new List<Cart> { cart });
        _mockDbContext.Setup(x => x.ProductVariants).ReturnsDbSet(new List<ProductVariant> { variant });
        _mockDbContext.Setup(x => x.CartItems).ReturnsDbSet(new List<CartItem>());

        // Act
        var result = await _sut.AddToCartAsync(cartId, variantId, quantity: 5);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateQuantityAsync_ValidQuantity_Updates()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var item = new CartItem
        {
            Id = 1, CartId = cartId, ProductVariantId = variantId,
            Quantity = 2, UnitPrice = 30m
        };

        var variant = new ProductVariant
        {
            Id = variantId, SalePrice = 30m,
            BranchOfficeStocks = new List<BranchOfficeStock>
            {
                new() { BranchOfficeId = 1, ProductVariantId = variantId, CurrentStock = 10 }
            }
        };

        _mockDbContext.Setup(x => x.CartItems).ReturnsDbSet(new List<CartItem> { item });
        _mockDbContext.Setup(x => x.ProductVariants).ReturnsDbSet(new List<ProductVariant> { variant });
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.UpdateQuantityAsync(cartId, variantId, quantity: 5);

        // Assert
        result.Success.Should().BeTrue();
        item.Quantity.Should().Be(5);
    }

    [Fact]
    public async Task RemoveItemAsync_ExistingItem_Removes()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var item = new CartItem
        {
            Id = 1, CartId = cartId, ProductVariantId = variantId,
            Quantity = 2, UnitPrice = 30m
        };

        _mockDbContext.Setup(x => x.CartItems).ReturnsDbSet(new List<CartItem> { item });
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.RemoveItemAsync(cartId, variantId);

        // Assert
        result.Success.Should().BeTrue();
        _mockDbContext.Verify(x => x.CartItems.Remove(It.Is<CartItem>(ci => ci.ProductVariantId == variantId)), Times.Once);
    }

    [Fact]
    public async Task GetCartSummaryAsync_WithItems_ReturnsCorrectTotals()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var items = new List<CartItem>
        {
            new() { Id = 1, CartId = cartId, ProductVariantId = Guid.NewGuid(), Quantity = 2, UnitPrice = 50m },
            new() { Id = 2, CartId = cartId, ProductVariantId = Guid.NewGuid(), Quantity = 1, UnitPrice = 100m }
        };

        _mockDbContext.Setup(x => x.CartItems).ReturnsDbSet(items);

        // Act
        var result = await _sut.GetCartSummaryAsync(cartId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.ItemCount.Should().Be(3); // 2 + 1
        result.Data.Total.Should().Be(200m); // (2*50) + (1*100)
    }

    [Fact]
    public async Task ClearCartAsync_RemovesAllItems()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var items = new List<CartItem>
        {
            new() { Id = 1, CartId = cartId, ProductVariantId = Guid.NewGuid(), Quantity = 1, UnitPrice = 50m },
            new() { Id = 2, CartId = cartId, ProductVariantId = Guid.NewGuid(), Quantity = 2, UnitPrice = 30m }
        };

        _mockDbContext.Setup(x => x.CartItems).ReturnsDbSet(items);
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.ClearCartAsync(cartId);

        // Assert
        result.Success.Should().BeTrue();
        _mockDbContext.Verify(x => x.CartItems.RemoveRange(It.IsAny<IEnumerable<CartItem>>()), Times.Once);
    }
}

using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Storefront;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Storefront;
using Entegrasyon.Entity.Orders;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.UnitTest.Storefront;

public class CheckoutManagerTests
{
    private readonly Mock<IDbContextFactory<IntegrationDbContext>> _mockContextFactory;
    private readonly Mock<IntegrationDbContext> _mockDbContext;
    private readonly Mock<IOfficeStockManager> _mockStockManager;
    private readonly CheckoutManager _sut;

    public CheckoutManagerTests()
    {
        _mockDbContext = new Mock<IntegrationDbContext>(
            new DbContextOptionsBuilder<IntegrationDbContext>().Options);
        _mockContextFactory = new Mock<IDbContextFactory<IntegrationDbContext>>();
        _mockContextFactory
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockDbContext.Object);

        _mockStockManager = new Mock<IOfficeStockManager>();

        _sut = new CheckoutManager(_mockContextFactory.Object, _mockStockManager.Object);
    }

    [Fact]
    public async Task CreateOrderFromCartAsync_ValidCart_CreatesOrderAndClearsCart()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variant = new ProductVariant
        {
            Id = variantId, ProductId = productId, SalePrice = 100m, Barcode = "BARCODE1",
            Product = new Product { Id = productId, Title = "Test Product" },
            BranchOfficeStocks = new List<BranchOfficeStock>
            {
                new() { BranchOfficeId = 1, ProductVariantId = variantId, CurrentStock = 10 }
            }
        };
        var cartItem = new CartItem
        {
            Id = 1, CartId = cartId, ProductVariantId = variantId,
            ProductVariant = variant,
            Quantity = 2, UnitPrice = 100m
        };
        var cart = new Cart
        {
            Id = cartId, TenantId = 1, CustomerId = 5,
            Items = new List<CartItem> { cartItem },
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };

        _mockDbContext.Setup(x => x.Carts).ReturnsDbSet(new List<Cart> { cart });
        _mockDbContext.Setup(x => x.Orders).ReturnsDbSet(new List<Order>());
        _mockDbContext.Setup(x => x.CartItems).ReturnsDbSet(new List<CartItem> { cartItem });
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockStockManager
            .Setup(x => x.DecreaseStockAtomicAsync(
                It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<int>(),
                It.IsAny<StockMovementType>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(new SuccessDataResult<StockMovement>(new StockMovement()));

        var dto = new CheckoutRequestDto(
            ShippingFullName: "John Doe", ShippingPhone: "555-0100",
            ShippingCity: "Istanbul", ShippingDistrict: "Kadikoy",
            ShippingAddress: "Test Mah. Test Sok. No:1",
            ShippingPostalCode: "34000", UseSameAddressForBilling: true,
            BillingFullName: null, BillingCity: null, BillingAddress: null,
            OrderNote: "Test note");

        // Act
        var result = await _sut.CreateOrderFromCartAsync(
            cartId, customerId: 5, tenantId: 1, dto,
            freeShippingThreshold: 150m, flatShippingRate: 29.90m);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.CustomerId.Should().Be(5);
        result.Data.StorefrontPaymentStatus.Should().Be(PaymentStatus.Paid);
        result.Data.StorefrontOrderStatus.Should().Be(OrderStatus.Received);
        result.Data.SubTotal.Should().Be(200m); // 2 * 100
        result.Data.ShippingCost.Should().Be(0m); // 200 >= 150 threshold => free
        result.Data.OrderNote.Should().Be("Test note");

        _mockStockManager.Verify(x => x.DecreaseStockAtomicAsync(
            1, variantId, 2, StockMovementType.Sale,
            "StorefrontOrder", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrderFromCartAsync_EmptyCart_ReturnsError()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var cart = new Cart
        {
            Id = cartId, TenantId = 1, CustomerId = 5,
            Items = new List<CartItem>(),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };

        _mockDbContext.Setup(x => x.Carts).ReturnsDbSet(new List<Cart> { cart });

        var dto = new CheckoutRequestDto(
            "John Doe", "555-0100", "Istanbul", "Kadikoy",
            "Test Address", "34000", true, null, null, null, null);

        // Act
        var result = await _sut.CreateOrderFromCartAsync(
            cartId, customerId: 5, tenantId: 1, dto,
            freeShippingThreshold: 150m, flatShippingRate: 29.90m);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CreateOrderFromCartAsync_CartNotFound_ReturnsError()
    {
        // Arrange
        _mockDbContext.Setup(x => x.Carts).ReturnsDbSet(new List<Cart>());

        var dto = new CheckoutRequestDto(
            "John Doe", "555-0100", "Istanbul", "Kadikoy",
            "Test Address", "34000", true, null, null, null, null);

        // Act
        var result = await _sut.CreateOrderFromCartAsync(
            Guid.NewGuid(), customerId: 5, tenantId: 1, dto,
            freeShippingThreshold: 150m, flatShippingRate: 29.90m);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CreateOrderFromCartAsync_BelowFreeShipping_ChargesShipping()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variant = new ProductVariant
        {
            Id = variantId, ProductId = productId, SalePrice = 50m, Barcode = "BARCODE2",
            Product = new Product { Id = productId, Title = "Cheap Product" },
            BranchOfficeStocks = new List<BranchOfficeStock>
            {
                new() { BranchOfficeId = 1, ProductVariantId = variantId, CurrentStock = 10 }
            }
        };
        var cartItem = new CartItem
        {
            Id = 1, CartId = cartId, ProductVariantId = variantId,
            ProductVariant = variant,
            Quantity = 1, UnitPrice = 50m
        };
        var cart = new Cart
        {
            Id = cartId, TenantId = 1, CustomerId = 5,
            Items = new List<CartItem> { cartItem },
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };

        _mockDbContext.Setup(x => x.Carts).ReturnsDbSet(new List<Cart> { cart });
        _mockDbContext.Setup(x => x.Orders).ReturnsDbSet(new List<Order>());
        _mockDbContext.Setup(x => x.CartItems).ReturnsDbSet(new List<CartItem> { cartItem });
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockStockManager
            .Setup(x => x.DecreaseStockAtomicAsync(
                It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<int>(),
                It.IsAny<StockMovementType>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(new SuccessDataResult<StockMovement>(new StockMovement()));

        var dto = new CheckoutRequestDto(
            "Jane Doe", "555-0200", "Ankara", "Cankaya",
            "Test Address 2", "06000", true, null, null, null, null);

        // Act
        var result = await _sut.CreateOrderFromCartAsync(
            cartId, customerId: 5, tenantId: 1, dto,
            freeShippingThreshold: 150m, flatShippingRate: 29.90m);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.SubTotal.Should().Be(50m);
        result.Data.ShippingCost.Should().Be(29.90m);
    }
}

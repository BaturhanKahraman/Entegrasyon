using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Storefront;
using Entegrasyon.Business.FeatureFlags;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity.Dtos.Storefront;
using Entegrasyon.Entity.Orders;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Entegrasyon.UnitTest.Storefront;

public class StorefrontFaz2Tests
{
    private readonly Mock<IDbContextFactory<IntegrationDbContext>> _mockContextFactory = new();
    private readonly Mock<IntegrationDbContext> _mockDbContext;

    public StorefrontFaz2Tests()
    {
        _mockDbContext = new Mock<IntegrationDbContext>(
            new DbContextOptionsBuilder<IntegrationDbContext>().Options);
        _mockContextFactory
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockDbContext.Object);
    }

    // ── Buy Again ──

    [Fact]
    public async Task GetPreviouslyPurchasedProductsAsync_ReturnsDistinctProducts()
    {
        // Arrange
        var product = new Product { Id = Guid.NewGuid(), Title = "Test Urun", SeoSlug = "test-urun" };
        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            Product = product,
            ProductId = product.Id,
            SalePrice = 100m,
            Images = new List<Image>(),
            BranchOfficeStocks = new List<BranchOfficeStock>
            {
                new() { CurrentStock = 10 }
            }
        };

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = 1,
            OrderDate = DateTimeOffset.UtcNow.AddDays(-5),
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-5)
        };

        var orderItems = new List<OrderItem>
        {
            new() { Id = 1, OrderId = order.Id, Order = order, ProductId = variant.Id, Quantity = 2, UnitPrice = 100 },
            new() { Id = 2, OrderId = order.Id, Order = order, ProductId = variant.Id, Quantity = 1, UnitPrice = 100 } // same variant
        };

        _mockDbContext.Setup(x => x.OrderItems).ReturnsDbSet(orderItems);
        _mockDbContext.Setup(x => x.ProductVariants).ReturnsDbSet(new List<ProductVariant> { variant });

        var manager = new OrderManager(
            _mockContextFactory.Object,
            Mock.Of<IOfficeStockManager>(),
            Mock.Of<INotificationManager>(),
            Mock.Of<ILogger<OrderManager>>(),
            Options.Create(new NotificationFeatureFlags { PublishEnabled = false }));

        // Act
        var result = await manager.GetPreviouslyPurchasedProductsAsync(1, 24);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    // ── Save For Later ──

    [Fact]
    public async Task SaveForLaterAsync_MovesItemFromCartToSaved()
    {
        // Arrange
        var variantId = Guid.NewGuid();
        var cartId = Guid.NewGuid();
        var cartItem = new CartItem
        {
            CartId = cartId,
            ProductVariantId = variantId,
            Quantity = 2,
            UnitPrice = 150m,
            AddedAt = DateTimeOffset.UtcNow
        };

        _mockDbContext.Setup(x => x.CartItems).ReturnsDbSet(new List<CartItem> { cartItem });
        _mockDbContext.Setup(x => x.StorefrontSavedCartItems).ReturnsDbSet(new List<StorefrontSavedCartItem>());
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var manager = new CartManager(_mockContextFactory.Object);

        // Act
        var result = await manager.SaveForLaterAsync(cartId, variantId, 1, 1);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetSavedItemsAsync_ReturnsSavedItems()
    {
        // Arrange
        var product = new Product { Id = Guid.NewGuid(), Title = "Kayitli Urun" };
        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            Product = product,
            SalePrice = 200m,
            Images = new List<Image>()
        };

        var savedItem = new StorefrontSavedCartItem
        {
            Id = 1,
            TenantId = 1,
            CustomerId = 1,
            ProductVariantId = variant.Id,
            ProductVariant = variant,
            OriginalPrice = 180m,
            SavedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        _mockDbContext.Setup(x => x.StorefrontSavedCartItems)
            .ReturnsDbSet(new List<StorefrontSavedCartItem> { savedItem });

        var manager = new CartManager(_mockContextFactory.Object);

        // Act
        var result = await manager.GetSavedItemsAsync(1, 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data[0].ProductTitle.Should().Be("Kayitli Urun");
        result.Data[0].OriginalPrice.Should().Be(180m);
        result.Data[0].CurrentPrice.Should().Be(200m);
    }

    [Fact]
    public async Task MoveToCartAsync_MovesItemBackToCart()
    {
        // Arrange
        var variantId = Guid.NewGuid();
        var cartId = Guid.NewGuid();

        var savedItem = new StorefrontSavedCartItem
        {
            Id = 1,
            TenantId = 1,
            CustomerId = 1,
            ProductVariantId = variantId,
            OriginalPrice = 150m,
            SavedAt = DateTimeOffset.UtcNow
        };

        var variant = new ProductVariant
        {
            Id = variantId,
            SalePrice = 160m,
            BranchOfficeStocks = new List<BranchOfficeStock>
            {
                new() { CurrentStock = 5 }
            }
        };

        _mockDbContext.Setup(x => x.StorefrontSavedCartItems)
            .ReturnsDbSet(new List<StorefrontSavedCartItem> { savedItem });
        _mockDbContext.Setup(x => x.ProductVariants)
            .ReturnsDbSet(new List<ProductVariant> { variant });
        _mockDbContext.Setup(x => x.CartItems)
            .ReturnsDbSet(new List<CartItem>());
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var manager = new CartManager(_mockContextFactory.Object);

        // Act
        var result = await manager.MoveToCartAsync(1, 1, variantId, cartId);

        // Assert
        result.Success.Should().BeTrue();
    }

    // ── Login History ──

    [Fact]
    public async Task RecordLoginAttemptAsync_SavesHistory()
    {
        // Arrange
        _mockDbContext.Setup(x => x.StorefrontLoginHistories)
            .ReturnsDbSet(new List<StorefrontLoginHistory>());
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var manager = new StorefrontAuthManager(_mockContextFactory.Object, Moq.Mock.Of<Entegrasyon.Business.Abstract.IStorefrontEmailService>(), Microsoft.Extensions.Options.Options.Create(new Entegrasyon.Business.FeatureFlags.NotificationFeatureFlags { PublishEnabled = false }));

        // Act & Assert — should not throw
        await manager.RecordLoginAttemptAsync(1, "192.168.1.1", "Mozilla/5.0", true);
    }

    [Fact]
    public async Task GetLoginHistoryAsync_ReturnsHistory()
    {
        // Arrange
        var history = new List<StorefrontLoginHistory>
        {
            new()
            {
                Id = 1, AuthId = 1, IpAddress = "192.168.1.1",
                DeviceType = "Masaustu", LoginAt = DateTimeOffset.UtcNow,
                IsSuccessful = true
            },
            new()
            {
                Id = 2, AuthId = 1, IpAddress = "10.0.0.1",
                DeviceType = "Mobil", LoginAt = DateTimeOffset.UtcNow.AddHours(-1),
                IsSuccessful = false, FailureReason = "Hatali sifre"
            }
        };

        _mockDbContext.Setup(x => x.StorefrontLoginHistories)
            .ReturnsDbSet(history);

        var manager = new StorefrontAuthManager(_mockContextFactory.Object, Moq.Mock.Of<Entegrasyon.Business.Abstract.IStorefrontEmailService>(), Microsoft.Extensions.Options.Options.Create(new Entegrasyon.Business.FeatureFlags.NotificationFeatureFlags { PublishEnabled = false }));

        // Act
        var result = await manager.GetLoginHistoryAsync(1, 20);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }

    // ── KVKK Data Export ──

    [Fact]
    public async Task ExportCustomerDataAsync_ReturnsNonEmptyJson()
    {
        // Arrange
        var auth = new StorefrontCustomerAuth
        {
            Id = 1, TenantId = 1, CustomerId = 10, Email = "test@test.com",
            PasswordHash = new byte[64], PasswordSalt = new byte[128],
            KvkkConsentDate = DateTimeOffset.UtcNow.AddMonths(-6),
            CreatedAt = DateTimeOffset.UtcNow.AddMonths(-6)
        };

        var customer = new Customer
        {
            Id = 10, Name = "Ali", Surname = "Yilmaz",
            PhoneNumber = "05551234567",
            Address = new Address { FullAddress = "Test Mahallesi", County = "Kadikoy", City = "Istanbul" }
        };

        _mockDbContext.Setup(x => x.StorefrontCustomerAuths)
            .ReturnsDbSet(new List<StorefrontCustomerAuth> { auth });
        _mockDbContext.Setup(x => x.Customers)
            .ReturnsDbSet(new List<Customer> { customer });
        _mockDbContext.Setup(x => x.Orders)
            .ReturnsDbSet(new List<Order>());
        _mockDbContext.Setup(x => x.StorefrontReviews)
            .ReturnsDbSet(new List<StorefrontReview>());
        _mockDbContext.Setup(x => x.StorefrontWishlistItems)
            .ReturnsDbSet(new List<StorefrontWishlistItem>());
        _mockDbContext.Setup(x => x.StorefrontLoginHistories)
            .ReturnsDbSet(new List<StorefrontLoginHistory>());

        var manager = new StorefrontAuthManager(_mockContextFactory.Object, Moq.Mock.Of<Entegrasyon.Business.Abstract.IStorefrontEmailService>(), Microsoft.Extensions.Options.Options.Create(new Entegrasyon.Business.FeatureFlags.NotificationFeatureFlags { PublishEnabled = false }));

        // Act
        var result = await manager.ExportCustomerDataAsync(1, 10);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNullOrEmpty();
        result.Data.Should().Contain("test@test.com");
        result.Data.Should().Contain("Ali");
    }
}

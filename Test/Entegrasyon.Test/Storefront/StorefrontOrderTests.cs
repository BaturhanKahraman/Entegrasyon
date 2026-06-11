using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.FeatureFlags;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Orders;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Entegrasyon.UnitTest.Storefront;

public class StorefrontOrderTests
{
    private readonly Mock<IDbContextFactory<IntegrationDbContext>> _mockContextFactory;
    private readonly Mock<IntegrationDbContext> _mockDbContext;
    private readonly Mock<IOfficeStockManager> _mockStockManager;
    private readonly Mock<INotificationManager> _mockNotificationManager;
    private readonly Mock<IApplicationLogManager> _mockApplicationLogManager;
    private readonly OrderManager _sut;

    public StorefrontOrderTests()
    {
        _mockDbContext = new Mock<IntegrationDbContext>(
            new DbContextOptionsBuilder<IntegrationDbContext>().Options);
        _mockContextFactory = new Mock<IDbContextFactory<IntegrationDbContext>>();
        _mockContextFactory
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockDbContext.Object);
        _mockContextFactory
            .Setup(f => f.CreateDbContext())
            .Returns(_mockDbContext.Object);

        _mockStockManager = new Mock<IOfficeStockManager>();
        _mockNotificationManager = new Mock<INotificationManager>();
        _mockApplicationLogManager = new Mock<IApplicationLogManager>();
        _mockApplicationLogManager
            .Setup(x => x.AddLog(It.IsAny<string>(), It.IsAny<LogType>(), It.IsAny<LogAction>(), It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Default: atomik stok artışı başarılı dönsün (testler ayrıca özelleştirebilir)
        _mockStockManager
            .Setup(s => s.IncreaseStockAtomicAsync(
                It.IsAny<int>(), It.IsAny<Guid>(), It.IsAny<int>(),
                It.IsAny<StockMovementType>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(new SuccessDataResult<StockMovement>(new StockMovement()));

        _sut = new OrderManager(
            _mockContextFactory.Object,
            _mockStockManager.Object,
            _mockNotificationManager.Object,
            _mockApplicationLogManager.Object,
            Mock.Of<ILogger<OrderManager>>(),
            Options.Create(new NotificationFeatureFlags { PublishEnabled = false }));
    }

    [Fact]
    public async Task CancelOrderAsync_StatusReceived_CancelsSuccessfully()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var customerId = 5;
        var variantId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            CustomerId = customerId,
            StorefrontOrderStatus = OrderStatus.Received,
            StorefrontPaymentStatus = PaymentStatus.Paid,
            OrderItems = new List<OrderItem>
            {
                new() { ProductId = variantId, Quantity = 2 }
            }
        };

        _mockDbContext.Setup(x => x.Orders).ReturnsDbSet(new List<Order> { order });
        _mockDbContext.Setup(x => x.BranchOfficeStocks).ReturnsDbSet(new List<BranchOfficeStock>());
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.CancelOrderAsync(orderId, customerId);

        // Assert
        result.Success.Should().BeTrue();
        order.StorefrontOrderStatus.Should().Be(OrderStatus.Cancelled);
        order.StorefrontPaymentStatus.Should().Be(PaymentStatus.Refunded);
    }

    [Fact]
    public async Task CancelOrderAsync_StatusShipped_ReturnsError()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var customerId = 5;
        var order = new Order
        {
            Id = orderId,
            CustomerId = customerId,
            StorefrontOrderStatus = OrderStatus.Shipped,
            OrderItems = new List<OrderItem>()
        };

        _mockDbContext.Setup(x => x.Orders).ReturnsDbSet(new List<Order> { order });

        // Act
        var result = await _sut.CancelOrderAsync(orderId, customerId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("iptal");
    }

    [Fact]
    public async Task CancelOrderAsync_WrongCustomerId_ReturnsError()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            CustomerId = 5,
            StorefrontOrderStatus = OrderStatus.Received,
            OrderItems = new List<OrderItem>()
        };

        _mockDbContext.Setup(x => x.Orders).ReturnsDbSet(new List<Order> { order });

        // Act
        var result = await _sut.CancelOrderAsync(orderId, customerId: 99);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CancelOrderAsync_OrderNotFound_ReturnsError()
    {
        // Arrange
        _mockDbContext.Setup(x => x.Orders).ReturnsDbSet(new List<Order>());

        // Act
        var result = await _sut.CancelOrderAsync(Guid.NewGuid(), customerId: 5);

        // Assert
        result.Success.Should().BeFalse();
    }

    // ── RED: storefront iptal yolu IncreaseStockAtomicAsync'i çağırmalı ──
    [Fact]
    public async Task CancelOrderAsync_WithItems_CallsIncreaseStockAtomicAsync_NotDirectMutation()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var customerId = 5;
        var variantId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            CustomerId = customerId,
            StorefrontOrderStatus = OrderStatus.Received,
            StorefrontPaymentStatus = PaymentStatus.Paid,
            OrderItems = new List<OrderItem>
            {
                new() { ProductId = variantId, Quantity = 3 }
            }
        };

        _mockDbContext.Setup(x => x.Orders).ReturnsDbSet(new List<Order> { order });
        _mockDbContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mockStockManager
            .Setup(s => s.IncreaseStockAtomicAsync(
                It.IsAny<int>(), variantId, 3,
                It.IsAny<StockMovementType>(),
                It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(new SuccessDataResult<StockMovement>(new StockMovement()));

        // Act
        var result = await _sut.CancelOrderAsync(orderId, customerId);

        // Assert — hem başarılı hem de atomik stok manager'ı tetiklemeli
        result.Success.Should().BeTrue();

        _mockStockManager.Verify(
            s => s.IncreaseStockAtomicAsync(
                It.IsAny<int>(),
                variantId,
                3,
                StockMovementType.Return,
                "StorefrontOrderCancellation",
                orderId.ToString()),
            Times.Once,
            "Storefront sipariş iptali stok artışını IncreaseStockAtomicAsync üzerinden yapmalı");

        // BranchOfficeStocks direkt okunmamalı/mutasyon edilmemeli
        _mockDbContext.Verify(
            x => x.BranchOfficeStocks,
            Times.Never,
            "Storefront iptal yolu BranchOfficeStocks'u doğrudan erişmemeli");
    }

    [Fact]
    public async Task GetCustomerOrdersAsync_ReturnsOnlyCustomerOrders()
    {
        // Arrange
        var customerId = 5;
        var order1 = new Order
        {
            Id = Guid.NewGuid(), CustomerId = customerId,
            OrderDate = DateTimeOffset.UtcNow, OrderItems = new List<OrderItem>()
        };
        var order2 = new Order
        {
            Id = Guid.NewGuid(), CustomerId = 99,
            OrderDate = DateTimeOffset.UtcNow, OrderItems = new List<OrderItem>()
        };

        _mockDbContext.Setup(x => x.Orders).ReturnsDbSet(new List<Order> { order1, order2 });

        // Act
        var result = await _sut.GetCustomerOrdersAsync(customerId, tenantId: 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data.First().CustomerId.Should().Be(customerId);
    }

    [Fact]
    public async Task GetOrderDetailAsync_CorrectCustomer_ReturnsOrder()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var customerId = 5;
        var order = new Order
        {
            Id = orderId, CustomerId = customerId,
            OrderItems = new List<OrderItem>()
        };

        _mockDbContext.Setup(x => x.Orders).ReturnsDbSet(new List<Order> { order });

        // Act
        var result = await _sut.GetOrderDetailAsync(orderId, customerId);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Id.Should().Be(orderId);
    }

    [Fact]
    public async Task GetOrderDetailAsync_WrongCustomer_ReturnsError()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId, CustomerId = 5,
            OrderItems = new List<OrderItem>()
        };

        _mockDbContext.Setup(x => x.Orders).ReturnsDbSet(new List<Order> { order });

        // Act
        var result = await _sut.GetOrderDetailAsync(orderId, customerId: 99);

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetOrderByNumberAsync_ExistingOrder_ReturnsOrder()
    {
        // Arrange
        var order = new Order
        {
            Id = Guid.NewGuid(), OrderNumber = "SF-20260326-ABCD1234",
            OrderItems = new List<OrderItem>()
        };

        _mockDbContext.Setup(x => x.Orders).ReturnsDbSet(new List<Order> { order });

        // Act
        var result = await _sut.GetOrderByNumberAsync("SF-20260326-ABCD1234");

        // Assert
        result.Success.Should().BeTrue();
        result.Data.OrderNumber.Should().Be("SF-20260326-ABCD1234");
    }

    [Fact]
    public async Task GetOrderByNumberAsync_NonExisting_ReturnsError()
    {
        // Arrange
        _mockDbContext.Setup(x => x.Orders).ReturnsDbSet(new List<Order>());

        // Act
        var result = await _sut.GetOrderByNumberAsync("INVALID");

        // Assert
        result.Success.Should().BeFalse();
    }
}

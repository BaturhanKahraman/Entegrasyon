using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.BackgroundServices;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.FeatureFlags;
using Entegrasyon.Entity.Dtos.N11;
using Entegrasyon.Entity.Orders;
using Entegrasyon.Entity.Products;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Entegrasyon.Test.N11;

/// <summary>
/// OrderManager.ImportN11OrdersAsync için birim testleri.
/// Advisory lock'tan bağımsız, ExecuteN11ImportAsync internal metodu üzerinden test edilir.
/// </summary>
public class N11OrderImportTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IOfficeStockManager> _mockOfficeStockManager = new();
    private readonly Mock<INotificationManager> _mockNotificationManager = new();
    private readonly Mock<ILogger<OrderManager>> _mockLogger = new();

    private static readonly IOptions<NotificationFeatureFlags> _disabledFlags =
        Options.Create(new NotificationFeatureFlags { PublishEnabled = false });

    private OrderManager CreateSut() => new(
        mockContextFactory.Object,
        _mockOfficeStockManager.Object,
        _mockNotificationManager.Object,
        Mock.Of<IApplicationLogManager>(),
        _mockLogger.Object,
        _disabledFlags);

    // -----------------------------------------------------------------------
    // Test 1: ImportN11OrdersAsync — Order.MarketPlaceId == 2 olmalı
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ImportN11OrdersAsync_ShouldCreateOrderWithMarketPlaceId2()
    {
        // Arrange
        var dto = BuildN11OrderDto("N11-TEST-001");
        var existingOrders = new List<Order>();
        var existingVariants = new List<ProductVariant>();

        SetupDbSets(existingOrders, existingVariants, warehouseIds: []);

        var capturedOrders = new List<Order>();
        mockIntegrationDbContext
            .Setup(x => x.Orders.Add(It.IsAny<Order>()))
            .Callback<Order>(capturedOrders.Add);

        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        var result = await sut.ExecuteN11ImportAsync(
            mockIntegrationDbContext.Object, [dto]);

        // Assert
        result.Success.Should().BeTrue();
        capturedOrders.Should().HaveCount(1);
        capturedOrders[0].MarketPlaceId.Should().Be(2);
        capturedOrders[0].OrderNumber.Should().Be("N11-TEST-001");
    }

    // -----------------------------------------------------------------------
    // Test 2: Aynı OrderNumber ile tekrar gelince atlanmalı (deduplication)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ImportN11OrdersAsync_WhenDuplicate_ShouldSkip()
    {
        // Arrange
        var existingOrder = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "N11-DUPE-001",
            MarketPlaceId = 2
        };

        var existingOrders = new List<Order> { existingOrder };
        var existingVariants = new List<ProductVariant>();

        SetupDbSets(existingOrders, existingVariants, warehouseIds: []);

        var addCalled = false;
        mockIntegrationDbContext
            .Setup(x => x.Orders.Add(It.IsAny<Order>()))
            .Callback<Order>(_ => addCalled = true);

        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var sut = CreateSut();
        var dto = BuildN11OrderDto("N11-DUPE-001");

        // Act
        var result = await sut.ExecuteN11ImportAsync(
            mockIntegrationDbContext.Object, [dto]);

        // Assert
        result.Success.Should().BeTrue();
        addCalled.Should().BeFalse("duplicate sipariş tekrar eklenmemeli");
    }

    // -----------------------------------------------------------------------
    // Test 3: Barkod eşleştiğinde OrderItem.ProductId doğru set edilmeli
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ImportN11OrdersAsync_ShouldMapBarcodeToProductVariant()
    {
        // Arrange
        var variantId = Guid.NewGuid();
        var variant = new ProductVariant { Id = variantId, Barcode = "SKU-BARKOD-007" };

        var existingOrders = new List<Order>();
        var existingVariants = new List<ProductVariant> { variant };

        SetupDbSets(existingOrders, existingVariants, warehouseIds: []);

        var capturedOrders = new List<Order>();
        mockIntegrationDbContext
            .Setup(x => x.Orders.Add(It.IsAny<Order>()))
            .Callback<Order>(capturedOrders.Add);

        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var dto = BuildN11OrderDto("N11-BARCODE-001", barcode: "SKU-BARKOD-007");
        var sut = CreateSut();

        // Act
        var result = await sut.ExecuteN11ImportAsync(
            mockIntegrationDbContext.Object, [dto]);

        // Assert
        result.Success.Should().BeTrue();
        capturedOrders.Should().HaveCount(1);
        var orderItems = capturedOrders[0].OrderItems.ToList();
        orderItems.Should().HaveCount(1);
        orderItems[0].ProductId.Should().Be(variantId);
        orderItems[0].Barcode.Should().Be("SKU-BARKOD-007");
    }

    // -----------------------------------------------------------------------
    // Test 4: Barkod eşleşmediğinde OrderItem.ProductId null olmalı
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ImportN11OrdersAsync_WhenBarcodeNotFound_ShouldSetProductIdToNull()
    {
        // Arrange
        var existingOrders = new List<Order>();
        var existingVariants = new List<ProductVariant>();

        SetupDbSets(existingOrders, existingVariants, warehouseIds: []);

        var capturedOrders = new List<Order>();
        mockIntegrationDbContext
            .Setup(x => x.Orders.Add(It.IsAny<Order>()))
            .Callback<Order>(capturedOrders.Add);

        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var dto = BuildN11OrderDto("N11-NOBARK-001", barcode: "BILINMEYEN-BARKOD");
        var sut = CreateSut();

        // Act
        var result = await sut.ExecuteN11ImportAsync(
            mockIntegrationDbContext.Object, [dto]);

        // Assert
        result.Success.Should().BeTrue();
        capturedOrders[0].OrderItems.First().ProductId.Should().BeNull();
    }

    // -----------------------------------------------------------------------
    // Test 5: Kargo (Shipment) bilgisi dolu gelince Order'a yazılmalı (T061)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ImportN11OrdersAsync_WhenShipmentPresent_ShouldMapCargoToOrder()
    {
        // Arrange
        var existingOrders = new List<Order>();
        var existingVariants = new List<ProductVariant>();

        SetupDbSets(existingOrders, existingVariants, warehouseIds: []);

        var capturedOrders = new List<Order>();
        mockIntegrationDbContext
            .Setup(x => x.Orders.Add(It.IsAny<Order>()))
            .Callback<Order>(capturedOrders.Add);

        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var shipment = new N11ShipmentDto("Sürat Kargo", "N11-TRK-98765", "SHP-001");
        var dto = BuildN11OrderDto("N11-CARGO-001", shipment: shipment);
        var sut = CreateSut();

        // Act
        var result = await sut.ExecuteN11ImportAsync(
            mockIntegrationDbContext.Object, [dto]);

        // Assert
        result.Success.Should().BeTrue();
        capturedOrders.Should().HaveCount(1);
        capturedOrders[0].CargoProviderName.Should().Be("Sürat Kargo");
        capturedOrders[0].CargoTrackingNumber.Should().Be("N11-TRK-98765");
    }

    // -----------------------------------------------------------------------
    // Test 6: Kargo (Shipment) yokken NRE olmamalı, alanlar null kalmalı
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ImportN11OrdersAsync_WhenShipmentNull_ShouldLeaveCargoFieldsNull()
    {
        // Arrange
        var existingOrders = new List<Order>();
        var existingVariants = new List<ProductVariant>();

        SetupDbSets(existingOrders, existingVariants, warehouseIds: []);

        var capturedOrders = new List<Order>();
        mockIntegrationDbContext
            .Setup(x => x.Orders.Add(It.IsAny<Order>()))
            .Callback<Order>(capturedOrders.Add);

        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var dto = BuildN11OrderDto("N11-NOCARGO-001", shipment: null);
        var sut = CreateSut();

        // Act
        var result = await sut.ExecuteN11ImportAsync(
            mockIntegrationDbContext.Object, [dto]);

        // Assert
        result.Success.Should().BeTrue();
        capturedOrders.Should().HaveCount(1);
        capturedOrders[0].CargoProviderName.Should().BeNull();
        capturedOrders[0].CargoTrackingNumber.Should().BeNull();
    }

    // -----------------------------------------------------------------------
    // Test 7: İlk non-null Shipment item'ı alınmalı (precedence kararı)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ImportN11OrdersAsync_WithMultipleItems_ShouldUseFirstNonNullShipment()
    {
        // Arrange
        var existingOrders = new List<Order>();
        var existingVariants = new List<ProductVariant>();

        SetupDbSets(existingOrders, existingVariants, warehouseIds: []);

        var capturedOrders = new List<Order>();
        mockIntegrationDbContext
            .Setup(x => x.Orders.Add(It.IsAny<Order>()))
            .Callback<Order>(capturedOrders.Add);

        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var dto = BuildN11OrderDto("N11-MULTI-001");
        dto = dto with
        {
            OrderItems =
            [
                new N11OrderItemDto
                {
                    Id = 1L, ProductSellerCode = "BARK-A", Quantity = 1, Price = 10m,
                    Shipment = null
                },
                new N11OrderItemDto
                {
                    Id = 2L, ProductSellerCode = "BARK-B", Quantity = 1, Price = 10m,
                    Shipment = new N11ShipmentDto("PTT Kargo", "FIRST-TRK", "SHP-B")
                },
                new N11OrderItemDto
                {
                    Id = 3L, ProductSellerCode = "BARK-C", Quantity = 1, Price = 10m,
                    Shipment = new N11ShipmentDto("Yurtiçi", "SECOND-TRK", "SHP-C")
                },
            ]
        };
        var sut = CreateSut();

        // Act
        var result = await sut.ExecuteN11ImportAsync(
            mockIntegrationDbContext.Object, [dto]);

        // Assert
        result.Success.Should().BeTrue();
        capturedOrders.Should().HaveCount(1);
        capturedOrders[0].CargoProviderName.Should().Be("PTT Kargo");
        capturedOrders[0].CargoTrackingNumber.Should().Be("FIRST-TRK");
    }

    // -----------------------------------------------------------------------
    // Polling service existence test
    // -----------------------------------------------------------------------

    [Fact]
    public void N11OrderPollingService_ShouldExist()
    {
        typeof(N11OrderPollingService).Should()
            .BeAssignableTo<Microsoft.Extensions.Hosting.BackgroundService>();
    }

    // -----------------------------------------------------------------------
    // Yardımcı metodlar
    // -----------------------------------------------------------------------

    private void SetupDbSets(
        List<Order> orders,
        List<ProductVariant> variants,
        List<int> warehouseIds)
    {
        mockIntegrationDbContext
            .Setup(x => x.Orders)
            .ReturnsDbSet(orders);

        mockIntegrationDbContext
            .Setup(x => x.ProductVariants)
            .ReturnsDbSet(variants);

        mockIntegrationDbContext
            .Setup(x => x.MarketPlaceWarehouses)
            .ReturnsDbSet(new List<Entegrasyon.Entity.MarketPlaceWarehouse>());
    }

    private static N11OrderDto BuildN11OrderDto(
        string orderNumber = "N11-MOCK-001",
        string status = "New",
        decimal totalAmount = 299.90m,
        string barcode = "SKU-001",
        int quantity = 2,
        decimal price = 149.95m,
        N11ShipmentDto? shipment = null) => new()
    {
        Id = 123456L,
        OrderNumber = orderNumber,
        Status = status,
        TotalAmount = totalAmount,
        CreateDate = DateTimeOffset.UtcNow,
        Buyer = new N11BuyerDto("Ahmet", "Yılmaz", "ahmet@test.com"),
        BillingAddress = new N11AddressDto("İstanbul", "Kadıköy", "Test Mah. No:1", "34000"),
        ShippingAddress = new N11AddressDto("İstanbul", "Kadıköy", "Test Mah. No:1", "34000"),
        OrderItems =
        [
            new N11OrderItemDto
            {
                Id = 9001L,
                ProductId = 5001L,
                ProductSellerCode = barcode,
                ProductName = "Test Ürünü",
                Quantity = quantity,
                Price = price,
                VatRate = 18m,
                Status = "New",
                Shipment = shipment
            }
        ]
    };
}

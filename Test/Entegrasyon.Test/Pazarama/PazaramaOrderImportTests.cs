using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete;
using Entegrasyon.Business.Concrete.Pazarama;
using Entegrasyon.Entity.Orders;
using Entegrasyon.Entity.Products;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Entegrasyon.Test.Pazarama;

/// <summary>
/// OrderManager.ImportPazaramaOrdersAsync için birim testleri.
/// Advisory lock'tan bağımsız, ExecutePazaramaImportAsync internal metodu üzerinden test edilir.
/// </summary>
public class PazaramaOrderImportTests : Entegrasyon.UnitTest.BaseTest
{
    private readonly Mock<IOfficeStockManager> _mockOfficeStockManager = new();
    private readonly Mock<INotificationManager> _mockNotificationManager = new();
    private readonly Mock<ILogger<OrderManager>> _mockLogger = new();

    private OrderManager CreateSut() => new(
        mockContextFactory.Object,
        _mockOfficeStockManager.Object,
        _mockNotificationManager.Object,
        _mockLogger.Object);

    // -----------------------------------------------------------------------
    // Test 1: Import Order.MarketPlaceId == 5 olmalı
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ImportPazaramaOrdersAsync_ShouldCreateOrderWithMarketPlaceId5()
    {
        // Arrange
        var dto = BuildPazaramaOrderDto(11111111L);
        var existingOrders = new List<Order>();
        var existingVariants = new List<ProductVariant>();

        SetupDbSets(existingOrders, existingVariants);

        var capturedOrders = new List<Order>();
        mockIntegrationDbContext
            .Setup(x => x.Orders.Add(It.IsAny<Order>()))
            .Callback<Order>(capturedOrders.Add);

        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        var result = await sut.ExecutePazaramaImportAsync(
            mockIntegrationDbContext.Object, [dto]);

        // Assert
        result.Success.Should().BeTrue();
        capturedOrders.Should().HaveCount(1);
        capturedOrders[0].MarketPlaceId.Should().Be(5);
        capturedOrders[0].OrderNumber.Should().Be("11111111");
    }

    // -----------------------------------------------------------------------
    // Test 2: Aynı OrderNumber ile tekrar gelince atlanmalı (deduplication)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ImportPazaramaOrdersAsync_WhenDuplicate_ShouldSkip()
    {
        // Arrange
        var existingOrder = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "22222222",
            MarketPlaceId = 5
        };

        var existingOrders = new List<Order> { existingOrder };
        var existingVariants = new List<ProductVariant>();

        SetupDbSets(existingOrders, existingVariants);

        var addCalled = false;
        mockIntegrationDbContext
            .Setup(x => x.Orders.Add(It.IsAny<Order>()))
            .Callback<Order>(_ => addCalled = true);

        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var sut = CreateSut();
        var dto = BuildPazaramaOrderDto(22222222L);

        // Act
        var result = await sut.ExecutePazaramaImportAsync(
            mockIntegrationDbContext.Object, [dto]);

        // Assert
        result.Success.Should().BeTrue();
        addCalled.Should().BeFalse("duplicate sipariş tekrar eklenmemeli");
    }

    // -----------------------------------------------------------------------
    // Test 3: Barkod eşleştiğinde OrderItem.ProductId doğru set edilmeli
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ImportPazaramaOrdersAsync_ShouldMapBarcodeToProductVariant()
    {
        // Arrange
        var variantId = Guid.NewGuid();
        var variant = new ProductVariant { Id = variantId, Barcode = "PAZ-SKU-007" };

        var existingOrders = new List<Order>();
        var existingVariants = new List<ProductVariant> { variant };

        SetupDbSets(existingOrders, existingVariants);

        var capturedOrders = new List<Order>();
        mockIntegrationDbContext
            .Setup(x => x.Orders.Add(It.IsAny<Order>()))
            .Callback<Order>(capturedOrders.Add);

        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var dto = BuildPazaramaOrderDto(33333333L, barcode: "PAZ-SKU-007");
        var sut = CreateSut();

        // Act
        var result = await sut.ExecutePazaramaImportAsync(
            mockIntegrationDbContext.Object, [dto]);

        // Assert
        result.Success.Should().BeTrue();
        capturedOrders.Should().HaveCount(1);
        var orderItems = capturedOrders[0].OrderItems.ToList();
        orderItems.Should().HaveCount(1);
        orderItems[0].ProductId.Should().Be(variantId);
        orderItems[0].Barcode.Should().Be("PAZ-SKU-007");
    }

    // -----------------------------------------------------------------------
    // Test 4: Barkod eşleşmediğinde OrderItem.ProductId null olmalı
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ImportPazaramaOrdersAsync_WhenBarcodeNotFound_ShouldSetProductIdToNull()
    {
        // Arrange
        var existingOrders = new List<Order>();
        var existingVariants = new List<ProductVariant>();

        SetupDbSets(existingOrders, existingVariants);

        var capturedOrders = new List<Order>();
        mockIntegrationDbContext
            .Setup(x => x.Orders.Add(It.IsAny<Order>()))
            .Callback<Order>(capturedOrders.Add);

        mockIntegrationDbContext
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var dto = BuildPazaramaOrderDto(44444444L, barcode: "BILINMEYEN-BARKOD");
        var sut = CreateSut();

        // Act
        var result = await sut.ExecutePazaramaImportAsync(
            mockIntegrationDbContext.Object, [dto]);

        // Assert
        result.Success.Should().BeTrue();
        capturedOrders[0].OrderItems.First().ProductId.Should().BeNull();
    }

    // -----------------------------------------------------------------------
    // Yardımcı metodlar
    // -----------------------------------------------------------------------

    private void SetupDbSets(
        List<Order> orders,
        List<ProductVariant> variants)
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

    private static PazaramaOrderDto BuildPazaramaOrderDto(
        long orderNumber = 12345678L,
        int orderStatus = 1,
        decimal orderAmount = 299.99m,
        string barcode = "PAZ-SKU-001",
        int quantity = 1,
        decimal salePrice = 299.99m) => new(
            OrderId: "ORD-" + orderNumber,
            OrderNumber: orderNumber,
            OrderDate: "2024-01-15",
            OrderAmount: orderAmount,
            ShipmentAmount: 0m,
            DiscountAmount: 0m,
            DiscountDescription: null,
            Currency: "TRY",
            PaymentType: 1,
            OrderStatus: orderStatus,
            CustomerId: "CUST-001",
            CustomerName: "Test Müşteri",
            CustomerEmail: "test@pazarama.com",
            ShipmentAddress: new PazaramaOrderAddressDto(
                AddressId: "ADDR-1",
                Title: "Ev",
                NameSurname: "Test Müşteri",
                CustomerEmail: "test@pazarama.com",
                CityName: "İstanbul",
                DistrictName: "Kadıköy",
                NeighborhoodName: "Test Mah.",
                AddressDetail: "Test Sok. No:1",
                DisplayAddressText: "Test Sok. No:1 Kadıköy/İstanbul",
                PhoneNumber: "05001234567"),
            BillingAddress: new PazaramaOrderBillingAddressDto(
                AddressId: "BILL-1",
                Title: "Fatura",
                NameSurname: "Test Müşteri",
                CustomerEmail: "test@pazarama.com",
                CityName: "İstanbul",
                DistrictName: "Kadıköy",
                NeighborhoodName: "Test Mah.",
                AddressDetail: "Test Sok. No:1",
                DisplayAddressText: "Test Sok. No:1 Kadıköy/İstanbul",
                PhoneNumber: "05001234567",
                IdentityNumber: null,
                InvoiceType: null,
                CompanyName: null,
                TaxNumber: null,
                TaxOffice: null,
                IsEInvoiceObliged: null),
            Items:
            [
                new PazaramaOrderItemDto(
                    OrderItemId: "ITEM-001",
                    OrderItemStatus: orderStatus,
                    ShipmentCode: null,
                    ShipmentCost: null,
                    DeliveryType: 1,
                    DeliveryDetail: null,
                    Quantity: quantity,
                    ListPrice: new PazaramaMoneyDto(salePrice, (int)salePrice, salePrice.ToString(), "TRY"),
                    SalePrice: new PazaramaMoneyDto(salePrice, (int)salePrice, salePrice.ToString(), "TRY"),
                    TaxAmount: null,
                    ShipmentAmount: null,
                    TotalPrice: null,
                    DiscountAmount: null,
                    DiscountDescription: null,
                    TaxIncluded: true,
                    Cargo: null,
                    Product: new PazaramaOrderProductDto(
                        ProductId: "PROD-001",
                        Name: "Test Ürünü",
                        Title: "Test Ürünü Başlığı",
                        Url: null,
                        ImageUrl: null,
                        VariantOptionDisplay: null,
                        StockCode: barcode,
                        Code: barcode,
                        VatRate: 18))
            ]);
}

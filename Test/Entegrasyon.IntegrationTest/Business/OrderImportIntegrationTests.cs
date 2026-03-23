using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Trendyol;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// Siparis import integration testleri — gercek DB ile Trendyol siparis import akisi.
/// </summary>
[Trait("Category", "Integration")]
public class OrderImportIntegrationTests : IntegrationTestBase
{
    public OrderImportIntegrationTests(PostgreSqlFixture pgFixture) : base(pgFixture) { }

    private static TrendyolShipmentPackage BuildTrendyolPackage(long packageId, string orderNumber, string barcode = "1111111111111")
        => new(
            ShipmentPackageId: packageId,
            OrderNumber: orderNumber,
            OrderDate: DateTimeOffset.UtcNow.ToString("o"),
            Status: "Created",
            GrossAmount: 200,
            TotalDiscount: 20,
            TotalPrice: 180,
            Micro: false,
            FastDelivery: false,
            EstimatedDeliveryEndDate: null,
            CargoProviderInfo: new TrendyolCargoInfo("Aras Kargo", null, null),
            CustomerInfo: new TrendyolCustomerInfo("Test", "Musteri", "test@test.com"),
            ShipmentAddress: new TrendyolAddressInfo("Istanbul", "Kadikoy", "Test Adres", "34000", "TR"),
            InvoiceAddress: new TrendyolAddressInfo("Istanbul", "Kadikoy", "Test Fatura Adres", "34000", "TR"),
            Lines:
            [
                new TrendyolOrderLine(
                    LineId: 100 + packageId,
                    Quantity: 1,
                    Price: 200,
                    Discount: 20,
                    Barcode: barcode,
                    MerchantSku: "TST-SKU-001",
                    ProductName: "Test Urun",
                    ProductColor: "Siyah",
                    ProductSize: "M",
                    MerchantId: 12345)
            ]);

    [Fact]
    public async Task ImportTrendyolOrders_ShouldCreateOrders_InDatabase()
    {
        // Arrange
        var (orderManager, scope) = GetScopedService<IOrderManager>();
        using var _ = scope;

        var packages = new List<TrendyolShipmentPackage>
        {
            BuildTrendyolPackage(9001, "ORD-INT-001"),
            BuildTrendyolPackage(9002, "ORD-INT-002")
        };

        // Act
        var result = await orderManager.ImportTrendyolOrdersAsync(packages);

        // Assert
        result.Success.Should().BeTrue(result.Message);

        using var dbContext = CreateDbContext();
        var orders = await dbContext.Orders
            .Include(o => o.OrderItems)
            .Where(o => o.ShipmentPackageId == 9001 || o.ShipmentPackageId == 9002)
            .ToListAsync();

        orders.Should().HaveCount(2);
        orders.Should().AllSatisfy(o =>
        {
            o.OrderItems.Should().NotBeEmpty();
            o.MarketPlaceId.Should().Be(1, "Trendyol MarketPlaceId is 1");
        });
    }

    [Fact]
    public async Task ImportTrendyolOrders_ShouldBe_Idempotent()
    {
        // Arrange — ayni siparisi iki kez import et
        var (orderManager1, scope1) = GetScopedService<IOrderManager>();
        using var _1 = scope1;

        var packages = new List<TrendyolShipmentPackage>
        {
            BuildTrendyolPackage(9010, "ORD-DEDUP-001")
        };

        var result1 = await orderManager1.ImportTrendyolOrdersAsync(packages);
        result1.Success.Should().BeTrue(result1.Message);

        // Act — ayni packageId ile tekrar import
        var (orderManager2, scope2) = GetScopedService<IOrderManager>();
        using var _2 = scope2;
        var result2 = await orderManager2.ImportTrendyolOrdersAsync(packages);

        // Assert — hata olmamali, duplicate eklenmemeli
        result2.Success.Should().BeTrue(result2.Message);

        using var dbContext = CreateDbContext();
        var count = await dbContext.Orders.CountAsync(o => o.ShipmentPackageId == 9010);
        count.Should().Be(1, "Duplicate import should be skipped (deduplication by ShipmentPackageId)");
    }

    [Fact]
    public async Task GetOrdersAsync_ShouldReturnImportedOrders()
    {
        // Arrange — siparis import et
        var (orderManager, scope) = GetScopedService<IOrderManager>();
        using var _ = scope;

        await orderManager.ImportTrendyolOrdersAsync([
            BuildTrendyolPackage(9020, "ORD-LIST-001")
        ]);

        // Act
        var (orderManager2, scope2) = GetScopedService<IOrderManager>();
        using var _2 = scope2;
        var result = await orderManager2.GetOrdersAsync(marketPlaceId: 1);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Should().Contain(o => o.ShipmentPackageId == 9020);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_ShouldUpdateStatus()
    {
        // Arrange — siparis import et
        var (orderManager, scope) = GetScopedService<IOrderManager>();
        using var _ = scope;

        await orderManager.ImportTrendyolOrdersAsync([
            BuildTrendyolPackage(9030, "ORD-STATUS-001")
        ]);

        using var dbContext = CreateDbContext();
        var order = await dbContext.Orders.FirstAsync(o => o.ShipmentPackageId == 9030);

        // Act
        var (orderManager2, scope2) = GetScopedService<IOrderManager>();
        using var _2 = scope2;
        var result = await orderManager2.UpdateOrderStatusAsync(order.Id, "Shipped");

        // Assert
        result.Success.Should().BeTrue(result.Message);

        using var verifyContext = CreateDbContext();
        var updated = await verifyContext.Orders.FirstAsync(o => o.Id == order.Id);
        updated.MarketplaceOrderStatus.Should().Be("Shipped");
    }
}

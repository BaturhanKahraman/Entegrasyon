using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Dtos.Trendyol;
using Entegrasyon.Entity.Products;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// OrderItem.BrandName sipariş-anı snapshot'ı (T106) — gerçek DB ile.
///
/// Marka adı sipariş import edilirken ProductVariant→Product→Brand zincirinden OrderItem'a
/// kopyalanır. Marka sonradan yeniden adlandırılsa bile satış kaydı sipariş-anı markasını korur;
/// güncel ad GetOrderByIdAsync'in Brand Include'u ile ayrıca okunabilir ("eski (güncel)" gösterimi).
/// Eşleşmeyen (variant bulunamayan) satırlarda snapshot null kalır.
/// </summary>
[Trait("Category", "Integration")]
public class OrderItemBrandSnapshotIntegrationTests : IntegrationTestBase
{
    public OrderItemBrandSnapshotIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    private async Task<Guid> SeedBrandedVariantAsync(string brandName, string barcode)
    {
        using var db = CreateDbContext();
        var categoryId = await db.Categories.Select(c => c.Id).FirstAsync();

        var brand = new Brand
        {
            Name = brandName,
            NormalizedName = brandName.Trim().ToUpperInvariant(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Brands.Add(brand);
        await db.SaveChangesAsync();

        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        db.MainProducts.Add(new Product
        {
            Id = productId,
            Title = $"{brandName} Test Ürün",
            BrandId = brand.Id,
            CategoryId = categoryId,
            CreatedAt = DateTimeOffset.UtcNow
        });
        db.ProductVariants.Add(new ProductVariant
        {
            Id = variantId,
            ProductId = productId,
            Barcode = barcode,
            CurrencyType = "TRY",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
        return variantId;
    }

    private static TrendyolShipmentPackage BuildPackage(long packageId, string orderNumber, string barcode)
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
    public async Task ImportTrendyolOrders_WithMatchedBrandedVariant_CapturesBrandNameSnapshot()
    {
        // Arrange — markalı varyant + aynı barkodlu Trendyol siparişi
        const string barcode = "BR-SNAP-0001";
        await SeedBrandedVariantAsync("Nike", barcode);

        var (orderManager, scope) = GetScopedService<IOrderManager>();
        using var _ = scope;

        // Act
        var result = await orderManager.ImportTrendyolOrdersAsync([
            BuildPackage(9501, "ORD-SNAP-001", barcode)
        ]);

        // Assert — OrderItem.BrandName sipariş-anı markasını taşır
        result.Success.Should().BeTrue(result.Message);

        using var db = CreateDbContext();
        var item = await db.Orders
            .Where(o => o.ShipmentPackageId == 9501)
            .SelectMany(o => o.OrderItems)
            .FirstAsync();
        item.BrandName.Should().Be("Nike");
    }

    [Fact]
    public async Task ImportTrendyolOrders_WithUnmatchedBarcode_LeavesBrandNameNull()
    {
        // Arrange — eşleşen varyant YOK (rastgele barkod)
        var (orderManager, scope) = GetScopedService<IOrderManager>();
        using var _ = scope;

        // Act
        var result = await orderManager.ImportTrendyolOrdersAsync([
            BuildPackage(9502, "ORD-SNAP-002", "NO-MATCH-9999")
        ]);

        // Assert
        result.Success.Should().BeTrue(result.Message);

        using var db = CreateDbContext();
        var item = await db.Orders
            .Where(o => o.ShipmentPackageId == 9502)
            .SelectMany(o => o.OrderItems)
            .FirstAsync();
        item.BrandName.Should().BeNull();
    }

    [Fact]
    public async Task GetOrderById_AfterBrandRename_ExposesSnapshotAndCurrentName()
    {
        // Arrange — sipariş import et, sonra markayı yeniden adlandır
        const string barcode = "BR-SNAP-0003";
        await SeedBrandedVariantAsync("Nike", barcode);

        var (orderManager, scope) = GetScopedService<IOrderManager>();
        using var _ = scope;
        await orderManager.ImportTrendyolOrdersAsync([
            BuildPackage(9503, "ORD-SNAP-003", barcode)
        ]);

        Guid orderId;
        using (var db = CreateDbContext())
        {
            orderId = await db.Orders.Where(o => o.ShipmentPackageId == 9503).Select(o => o.Id).FirstAsync();

            var brand = await db.Brands.AsTracking().FirstAsync(b => b.Name == "Nike");
            brand.Name = "Nike Pro";
            brand.NormalizedName = "NIKE PRO";
            await db.SaveChangesAsync();
        }

        // Act — order detail sorgusu Brand zincirini yükler
        var (orderManager2, scope2) = GetScopedService<IOrderManager>();
        using var __ = scope2;
        var detail = await orderManager2.GetOrderByIdAsync(orderId);

        // Assert — snapshot eski adı korur, güncel ad zincirden okunur ("eski (güncel)")
        detail.Success.Should().BeTrue(detail.Message);
        var item = detail.Data!.OrderItems.First();
        item.BrandName.Should().Be("Nike", "snapshot sipariş-anı markasını korur");
        item.Product?.Product?.Brand?.Name.Should().Be("Nike Pro", "güncel marka adı Include ile yüklenir");
    }
}

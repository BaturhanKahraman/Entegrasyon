using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Orders;
using Entegrasyon.Entity.Products;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// ProductPerformanceManager aggregate matematiği — gerçek PostgreSQL ile.
///
/// Taban CategoryPerformanceManager ile aynı join zinciri ama filtre MainProduct (productId)
/// bazında ve "TopProducts" yerine "VariantBreakdown" üretir. Gerçek SQL semantiği (decimal,
/// DISTINCT order sayımı, NULL marketplace, varyant GroupBy) ancak gerçek Postgres'te
/// doğrulanabilir.
/// </summary>
[Trait("Category", "Integration")]
public class ProductPerformanceManagerIntegrationTests : IntegrationTestBase
{
    public ProductPerformanceManagerIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    // ── Seed helpers ────────────────────────────────────────────────────

    private static Address Addr() => new() { City = "Istanbul", Country = "TR", FullAddress = "x" };

    private async Task<int> SeedCategoryAsync(string name)
    {
        using var db = CreateDbContext();
        var cat = new Category { Name = name, CreatedAt = DateTimeOffset.UtcNow };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();
        return cat.Id;
    }

    /// <summary>Yeni bir MainProduct seed eder, Id'sini döner.</summary>
    private async Task<Guid> SeedProductAsync(int categoryId, string title)
    {
        using var db = CreateDbContext();
        var productId = Guid.NewGuid();
        db.MainProducts.Add(new Product
        {
            Id = productId,
            Title = title,
            CategoryId = categoryId,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
        return productId;
    }

    /// <summary>Verilen MainProduct'a bağlı varyant seed eder, varyant Id'sini döner.</summary>
    private async Task<Guid> SeedVariantAsync(Guid productId, string? name, string barcode)
    {
        using var db = CreateDbContext();
        var variantId = Guid.NewGuid();
        db.ProductVariants.Add(new ProductVariant
        {
            Id = variantId,
            ProductId = productId,
            Name = name,
            Barcode = barcode,
            CurrencyType = "TRY",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
        return variantId;
    }

    private async Task<Guid> SeedOrderWithItemsAsync(
        DateTimeOffset orderDate,
        int? marketPlaceId,
        params (Guid VariantId, int Qty, decimal UnitPrice, int Returned)[] items)
    {
        using var db = CreateDbContext();
        var orderId = Guid.NewGuid();
        db.Orders.Add(new Order
        {
            Id = orderId,
            OrderNumber = $"ORD-{orderId:N}"[..16],
            OrderDate = orderDate,
            MarketPlaceId = marketPlaceId,
            BillingAddress = Addr(),
            ShippingAddress = Addr()
        });
        foreach (var (variantId, qty, unitPrice, returned) in items)
        {
            db.OrderItems.Add(new OrderItem
            {
                OrderId = orderId,
                ProductId = variantId,
                Quantity = qty,
                UnitPrice = unitPrice,
                ReturnedQuantity = returned
            });
        }
        await db.SaveChangesAsync();
        return orderId;
    }

    private IProductPerformanceManager Sut(out IServiceScope scope)
    {
        var (svc, s) = GetScopedService<IProductPerformanceManager>();
        scope = s;
        return svc;
    }

    // ── Tests ───────────────────────────────────────────────────────────

    [Fact]
    public async Task NoSales_ReturnsAllZerosAndEmptyLists()
    {
        var categoryId = await SeedCategoryAsync("Kategori");
        var productId = await SeedProductAsync(categoryId, "Satışsız Ürün");
        await SeedVariantAsync(productId, "Kırmızı", "BC-NS-1");
        var sut = Sut(out var scope);
        using var _ = scope;

        var result = await sut.GetProductPerformanceAsync(productId, 30);

        result.ProductId.Should().Be(productId);
        result.TotalSoldQuantity.Should().Be(0);
        result.TotalRevenue.Should().Be(0);
        result.TotalOrderCount.Should().Be(0);
        result.TotalReturnedQuantity.Should().Be(0);
        result.ReturnRate.Should().Be(0);
        result.AverageUnitPrice.Should().Be(0);
        result.VariantBreakdown.Should().BeEmpty();
        result.MarketplaceBreakdown.Should().BeEmpty();
    }

    [Fact]
    public async Task AggregatesQuantityRevenueAndDistinctOrders()
    {
        var categoryId = await SeedCategoryAsync("Kategori");
        var productId = await SeedProductAsync(categoryId, "Ürün A");
        var variant = await SeedVariantAsync(productId, "Tek Beden", "BC-AGG-1");
        await SeedMarketPlaceAsync(1, "Trendyol");

        // order1: iki satır → DISTINCT order 1; qty 2+3, revenue 5*100
        await SeedOrderWithItemsAsync(DateTimeOffset.UtcNow.AddDays(-1), 1,
            (variant, 2, 100m, 0), (variant, 3, 100m, 0));
        // order2: qty 4
        await SeedOrderWithItemsAsync(DateTimeOffset.UtcNow.AddDays(-2), 1,
            (variant, 4, 100m, 0));

        var sut = Sut(out var scope);
        using var _ = scope;

        var result = await sut.GetProductPerformanceAsync(productId, 30);

        result.TotalSoldQuantity.Should().Be(9);
        result.TotalRevenue.Should().Be(900m);
        result.TotalOrderCount.Should().Be(2);
        result.AverageUnitPrice.Should().Be(100m);
    }

    [Fact]
    public async Task AverageUnitPrice_IsQuantityWeighted_NotLineAveraged()
    {
        var categoryId = await SeedCategoryAsync("Kategori");
        var productId = await SeedProductAsync(categoryId, "Ürün A");
        var variant = await SeedVariantAsync(productId, "Tek Beden", "BC-AVG-1");
        await SeedMarketPlaceAsync(1, "Trendyol");

        // Satır1: 1 adet * 10 TL ; Satır2: 9 adet * 110 TL
        // Ağırlıklı (doğru) = ciro/adet = (10 + 990) / 10 = 100 ; satır-ort (yanlış) = 60
        await SeedOrderWithItemsAsync(DateTimeOffset.UtcNow.AddDays(-1), 1,
            (variant, 1, 10m, 0), (variant, 9, 110m, 0));

        var sut = Sut(out var scope);
        using var _ = scope;

        var result = await sut.GetProductPerformanceAsync(productId, 30);

        result.TotalRevenue.Should().Be(1000m);
        result.AverageUnitPrice.Should().Be(100m);   // ağırlıklı; satır-ortalaması 60 OLMAMALI
    }

    [Fact]
    public async Task SubtractsReturnedQuantityAndComputesReturnRate()
    {
        var categoryId = await SeedCategoryAsync("Kategori");
        var productId = await SeedProductAsync(categoryId, "Ürün A");
        var variant = await SeedVariantAsync(productId, "Tek Beden", "BC-RET-1");
        await SeedMarketPlaceAsync(1, "Trendyol");
        await SeedOrderWithItemsAsync(DateTimeOffset.UtcNow.AddDays(-1), 1, (variant, 10, 50m, 2));

        var sut = Sut(out var scope);
        using var _ = scope;

        var result = await sut.GetProductPerformanceAsync(productId, 30);

        result.TotalSoldQuantity.Should().Be(8);          // 10 - 2
        result.TotalReturnedQuantity.Should().Be(2);
        result.TotalRevenue.Should().Be(500m);            // iade ciroyu düşürmez
        result.ReturnRate.Should().Be(20m);               // 2/10*100
    }

    [Fact]
    public async Task AllReturned_ReturnRateSafeNoDivideByZero()
    {
        var categoryId = await SeedCategoryAsync("Kategori");
        var productId = await SeedProductAsync(categoryId, "Ürün A");
        var variant = await SeedVariantAsync(productId, "Tek Beden", "BC-ZERO-1");
        await SeedMarketPlaceAsync(1, "Trendyol");
        await SeedOrderWithItemsAsync(DateTimeOffset.UtcNow.AddDays(-1), 1, (variant, 5, 20m, 5));

        var sut = Sut(out var scope);
        using var _ = scope;

        var result = await sut.GetProductPerformanceAsync(productId, 30);

        result.TotalSoldQuantity.Should().Be(0);
        result.ReturnRate.Should().Be(100m);              // tümü iade: 5/5*100
        result.TotalRevenue.Should().Be(100m);
    }

    [Fact]
    public async Task VariantBreakdown_OrderedByRevenueDescending()
    {
        var categoryId = await SeedCategoryAsync("Kategori");
        var productId = await SeedProductAsync(categoryId, "Çok Varyantlı Ürün");
        await SeedMarketPlaceAsync(1, "Trendyol");

        // 3 varyant, farklı ciro: Mavi 60, Yeşil 40, Kırmızı 20
        var kirmizi = await SeedVariantAsync(productId, "Kırmızı", "BC-VAR-R");
        var yesil = await SeedVariantAsync(productId, "Yeşil", "BC-VAR-G");
        var mavi = await SeedVariantAsync(productId, "Mavi", "BC-VAR-B");
        await SeedOrderWithItemsAsync(DateTimeOffset.UtcNow.AddDays(-1), 1,
            (kirmizi, 2, 10m, 0), (yesil, 4, 10m, 0), (mavi, 6, 10m, 0));

        var sut = Sut(out var scope);
        using var _ = scope;

        var result = await sut.GetProductPerformanceAsync(productId, 30);

        result.VariantBreakdown.Should().HaveCount(3);
        result.VariantBreakdown.Select(v => v.Revenue).Should().ContainInOrder(60m, 40m, 20m);
        result.VariantBreakdown[0].VariantName.Should().Be("Mavi");
        result.VariantBreakdown[0].SoldQuantity.Should().Be(6);
        result.VariantBreakdown[0].Barcode.Should().Be("BC-VAR-B");
    }

    [Fact]
    public async Task VariantName_FallsBackToBarcodeWhenNameNull()
    {
        var categoryId = await SeedCategoryAsync("Kategori");
        var productId = await SeedProductAsync(categoryId, "Ürün A");
        var variant = await SeedVariantAsync(productId, null, "BC-FALLBACK-1");
        await SeedMarketPlaceAsync(1, "Trendyol");
        await SeedOrderWithItemsAsync(DateTimeOffset.UtcNow.AddDays(-1), 1, (variant, 1, 100m, 0));

        var sut = Sut(out var scope);
        using var _ = scope;

        var result = await sut.GetProductPerformanceAsync(productId, 30);

        result.VariantBreakdown.Should().ContainSingle();
        result.VariantBreakdown[0].VariantName.Should().Be("BC-FALLBACK-1");  // Name null → Barcode
    }

    [Fact]
    public async Task MarketplaceBreakdown_NullMapsToDirektStorefront()
    {
        var categoryId = await SeedCategoryAsync("Kategori");
        var productId = await SeedProductAsync(categoryId, "Ürün A");
        var variant = await SeedVariantAsync(productId, "Tek Beden", "BC-MP-1");
        await SeedMarketPlaceAsync(1, "Trendyol");

        await SeedOrderWithItemsAsync(DateTimeOffset.UtcNow.AddDays(-1), 1, (variant, 2, 100m, 0));
        await SeedOrderWithItemsAsync(DateTimeOffset.UtcNow.AddDays(-1), null, (variant, 1, 100m, 0));

        var sut = Sut(out var scope);
        using var _ = scope;

        var result = await sut.GetProductPerformanceAsync(productId, 30);

        result.MarketplaceBreakdown.Should().HaveCount(2);

        var direkt = result.MarketplaceBreakdown.Single(m => m.MarketPlaceId == null);
        direkt.MarketplaceName.Should().Be("Direkt/Storefront");
        direkt.OrderCount.Should().Be(1);
        direkt.Revenue.Should().Be(100m);

        var trendyol = result.MarketplaceBreakdown.Single(m => m.MarketPlaceId == 1);
        trendyol.MarketplaceName.Should().Be("Trendyol");
        trendyol.Revenue.Should().Be(200m);
    }

    [Fact]
    public async Task ExcludesOrdersOutsideTimeWindow()
    {
        var categoryId = await SeedCategoryAsync("Kategori");
        var productId = await SeedProductAsync(categoryId, "Ürün A");
        var variant = await SeedVariantAsync(productId, "Tek Beden", "BC-TIME-1");
        await SeedMarketPlaceAsync(1, "Trendyol");

        await SeedOrderWithItemsAsync(DateTimeOffset.UtcNow.AddDays(-5), 1, (variant, 3, 10m, 0));
        await SeedOrderWithItemsAsync(DateTimeOffset.UtcNow.AddDays(-40), 1, (variant, 7, 10m, 0));

        var sut = Sut(out var scope);
        using var _ = scope;

        var result = await sut.GetProductPerformanceAsync(productId, daysPast: 30);

        result.TotalSoldQuantity.Should().Be(3);   // 40 günlük hariç
        result.TotalRevenue.Should().Be(30m);
    }

    [Fact]
    public async Task ExcludesOtherProducts()
    {
        var categoryId = await SeedCategoryAsync("Kategori");
        var targetProduct = await SeedProductAsync(categoryId, "Hedef Ürün");
        var otherProduct = await SeedProductAsync(categoryId, "Diğer Ürün");
        var targetVariant = await SeedVariantAsync(targetProduct, "Hedef Varyant", "BC-T-1");
        var otherVariant = await SeedVariantAsync(otherProduct, "Diğer Varyant", "BC-O-1");
        await SeedMarketPlaceAsync(1, "Trendyol");

        await SeedOrderWithItemsAsync(DateTimeOffset.UtcNow.AddDays(-1), 1,
            (targetVariant, 2, 10m, 0), (otherVariant, 99, 10m, 0));

        var sut = Sut(out var scope);
        using var _ = scope;

        var result = await sut.GetProductPerformanceAsync(targetProduct, 30);

        result.TotalSoldQuantity.Should().Be(2);   // diğer ürün hariç
        result.VariantBreakdown.Should().ContainSingle();
        result.VariantBreakdown[0].VariantName.Should().Be("Hedef Varyant");
    }
}

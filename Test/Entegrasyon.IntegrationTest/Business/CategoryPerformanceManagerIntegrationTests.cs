using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Orders;
using Entegrasyon.Entity.Products;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// CategoryPerformanceManager aggregate matematiği — gerçek PostgreSQL ile.
///
/// Bu sorgu 3-tablo navigation join (OrderItems → ProductVariants → MainProducts) + zaman
/// penceresi (Orders.OrderDate) + GroupBy/DISTINCT içerir. Gerçek SQL semantiği (decimal,
/// DISTINCT order sayımı, NULL marketplace) ancak gerçek Postgres'te doğrulanabilir —
/// InMemory full-model (NpgsqlTsVector) yüklenemez, Moq DbSet GroupBy/join'i çalıştırmaz.
/// </summary>
[Trait("Category", "Integration")]
public class CategoryPerformanceManagerIntegrationTests : IntegrationTestBase
{
    public CategoryPerformanceManagerIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
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

    private async Task<Guid> SeedVariantInCategoryAsync(int categoryId, string title, string barcode)
    {
        using var db = CreateDbContext();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        db.MainProducts.Add(new Product
        {
            Id = productId,
            Title = title,
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

    private ICategoryPerformanceManager Sut(out IServiceScope scope)
    {
        var (svc, s) = GetScopedService<ICategoryPerformanceManager>();
        scope = s;
        return svc;
    }

    // ── Tests ───────────────────────────────────────────────────────────

    [Fact]
    public async Task NoSales_ReturnsAllZerosAndEmptyLists()
    {
        var categoryId = await SeedCategoryAsync("Boş Kategori");
        var sut = Sut(out var scope);
        using var _ = scope;

        var result = await sut.GetCategoryPerformanceAsync(categoryId, 30);

        result.CategoryId.Should().Be(categoryId);
        result.TotalSoldQuantity.Should().Be(0);
        result.TotalRevenue.Should().Be(0);
        result.TotalOrderCount.Should().Be(0);
        result.TotalReturnedQuantity.Should().Be(0);
        result.ReturnRate.Should().Be(0);
        result.AverageUnitPrice.Should().Be(0);
        result.TopProducts.Should().BeEmpty();
        result.MarketplaceBreakdown.Should().BeEmpty();
    }

    [Fact]
    public async Task AggregatesQuantityRevenueAndDistinctOrders()
    {
        var categoryId = await SeedCategoryAsync("Satış Kategori");
        var variant = await SeedVariantInCategoryAsync(categoryId, "Ürün A", "BC-AGG-1");

        await SeedMarketPlaceAsync(1, "Trendyol");
        // order1: iki satır → DISTINCT order 1; qty 2+3, revenue 5*100
        await SeedOrderWithItemsAsync(DateTimeOffset.UtcNow.AddDays(-1), 1,
            (variant, 2, 100m, 0), (variant, 3, 100m, 0));
        // order2: qty 4
        await SeedOrderWithItemsAsync(DateTimeOffset.UtcNow.AddDays(-2), 1,
            (variant, 4, 100m, 0));

        var sut = Sut(out var scope);
        using var _ = scope;

        var result = await sut.GetCategoryPerformanceAsync(categoryId, 30);

        result.TotalSoldQuantity.Should().Be(9);
        result.TotalRevenue.Should().Be(900m);
        result.TotalOrderCount.Should().Be(2);
        result.AverageUnitPrice.Should().Be(100m);
    }

    [Fact]
    public async Task AverageUnitPrice_IsQuantityWeighted_NotLineAveraged()
    {
        var categoryId = await SeedCategoryAsync("Ağırlıklı Ortalama Kategori");
        var variant = await SeedVariantInCategoryAsync(categoryId, "Ürün A", "BC-AVG-1");
        await SeedMarketPlaceAsync(1, "Trendyol");

        // Satır1: 1 adet * 10 TL ; Satır2: 9 adet * 110 TL
        // Satır-ortalaması (yanlış) = (10+110)/2 = 60
        // Ağırlıklı (doğru) = ciro/adet = (10 + 990) / 10 = 100
        await SeedOrderWithItemsAsync(DateTimeOffset.UtcNow.AddDays(-1), 1,
            (variant, 1, 10m, 0), (variant, 9, 110m, 0));

        var sut = Sut(out var scope);
        using var _ = scope;

        var result = await sut.GetCategoryPerformanceAsync(categoryId, 30);

        result.TotalRevenue.Should().Be(1000m);
        result.AverageUnitPrice.Should().Be(100m);   // ağırlıklı; satır-ortalaması 60 OLMAMALI
    }

    [Fact]
    public async Task SubtractsReturnedQuantityAndComputesReturnRate()
    {
        var categoryId = await SeedCategoryAsync("İade Kategori");
        var variant = await SeedVariantInCategoryAsync(categoryId, "Ürün A", "BC-RET-1");
        await SeedMarketPlaceAsync(1, "Trendyol");
        await SeedOrderWithItemsAsync(DateTimeOffset.UtcNow.AddDays(-1), 1, (variant, 10, 50m, 2));

        var sut = Sut(out var scope);
        using var _ = scope;

        var result = await sut.GetCategoryPerformanceAsync(categoryId, 30);

        result.TotalSoldQuantity.Should().Be(8);          // 10 - 2
        result.TotalReturnedQuantity.Should().Be(2);
        result.TotalRevenue.Should().Be(500m);            // iade ciroyu düşürmez
        result.ReturnRate.Should().Be(20m);               // 2/10*100
    }

    [Fact]
    public async Task AllReturned_ReturnRateSafeNoDivideByZero()
    {
        var categoryId = await SeedCategoryAsync("Tam İade Kategori");
        var variant = await SeedVariantInCategoryAsync(categoryId, "Ürün A", "BC-ZERO-1");
        await SeedMarketPlaceAsync(1, "Trendyol");
        await SeedOrderWithItemsAsync(DateTimeOffset.UtcNow.AddDays(-1), 1, (variant, 5, 20m, 5));

        var sut = Sut(out var scope);
        using var _ = scope;

        var result = await sut.GetCategoryPerformanceAsync(categoryId, 30);

        result.TotalSoldQuantity.Should().Be(0);
        result.ReturnRate.Should().Be(100m);              // tümü iade: 5/5*100
        result.TotalRevenue.Should().Be(100m);
    }

    [Fact]
    public async Task TopProducts_OrderedByRevenueLimitedToFive()
    {
        var categoryId = await SeedCategoryAsync("Top5 Kategori");
        await SeedMarketPlaceAsync(1, "Trendyol");

        var variants = new List<(Guid VariantId, int Qty, decimal UnitPrice, int Returned)>();
        for (var i = 0; i < 6; i++)
        {
            var v = await SeedVariantInCategoryAsync(categoryId, $"Ürün {i}", $"BC-TOP-{i}");
            variants.Add((v, 6 - i, 10m, 0)); // ciro: 60,50,40,30,20,10
        }
        await SeedOrderWithItemsAsync(DateTimeOffset.UtcNow.AddDays(-1), 1, variants.ToArray());

        var sut = Sut(out var scope);
        using var _ = scope;

        var result = await sut.GetCategoryPerformanceAsync(categoryId, 30);

        result.TopProducts.Should().HaveCount(5);
        result.TopProducts.Select(p => p.Revenue).Should().ContainInOrder(60m, 50m, 40m, 30m, 20m);
        result.TopProducts[0].ProductName.Should().Be("Ürün 0");
        result.TopProducts[0].SoldQuantity.Should().Be(6);
    }

    [Fact]
    public async Task MarketplaceBreakdown_NullMapsToDirektStorefront()
    {
        var categoryId = await SeedCategoryAsync("Pazaryeri Kategori");
        var variant = await SeedVariantInCategoryAsync(categoryId, "Ürün A", "BC-MP-1");
        await SeedMarketPlaceAsync(1, "Trendyol");

        await SeedOrderWithItemsAsync(DateTimeOffset.UtcNow.AddDays(-1), 1, (variant, 2, 100m, 0));
        await SeedOrderWithItemsAsync(DateTimeOffset.UtcNow.AddDays(-1), null, (variant, 1, 100m, 0));

        var sut = Sut(out var scope);
        using var _ = scope;

        var result = await sut.GetCategoryPerformanceAsync(categoryId, 30);

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
        var categoryId = await SeedCategoryAsync("Zaman Kategori");
        var variant = await SeedVariantInCategoryAsync(categoryId, "Ürün A", "BC-TIME-1");
        await SeedMarketPlaceAsync(1, "Trendyol");

        await SeedOrderWithItemsAsync(DateTimeOffset.UtcNow.AddDays(-5), 1, (variant, 3, 10m, 0));
        await SeedOrderWithItemsAsync(DateTimeOffset.UtcNow.AddDays(-40), 1, (variant, 7, 10m, 0));

        var sut = Sut(out var scope);
        using var _ = scope;

        var result = await sut.GetCategoryPerformanceAsync(categoryId, daysPast: 30);

        result.TotalSoldQuantity.Should().Be(3);   // 40 günlük hariç
        result.TotalRevenue.Should().Be(30m);
    }

    [Fact]
    public async Task ExcludesOtherCategories()
    {
        var targetCategory = await SeedCategoryAsync("Hedef Kategori");
        var otherCategory = await SeedCategoryAsync("Diğer Kategori");
        var targetVariant = await SeedVariantInCategoryAsync(targetCategory, "Hedef Ürün", "BC-T-1");
        var otherVariant = await SeedVariantInCategoryAsync(otherCategory, "Diğer Ürün", "BC-O-1");
        await SeedMarketPlaceAsync(1, "Trendyol");

        await SeedOrderWithItemsAsync(DateTimeOffset.UtcNow.AddDays(-1), 1,
            (targetVariant, 2, 10m, 0), (otherVariant, 99, 10m, 0));

        var sut = Sut(out var scope);
        using var _ = scope;

        var result = await sut.GetCategoryPerformanceAsync(targetCategory, 30);

        result.TotalSoldQuantity.Should().Be(2);   // diğer kategori hariç
        result.TopProducts.Should().ContainSingle();
        result.TopProducts[0].ProductName.Should().Be("Hedef Ürün");
    }
}

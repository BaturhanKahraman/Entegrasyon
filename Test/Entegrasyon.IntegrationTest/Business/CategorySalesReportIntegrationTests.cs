using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Orders;
using Entegrasyon.Entity.Sales;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// Kategori Bazlı Satış analitiği — gerçek PostgreSQL.
/// Kanal: Mağaza = Sales/SaleItem; pazaryeri + Storefront = Orders/OrderItem.
///
/// Kapsam: kanal karşılaştırma, sezonsal (geçen yıl), yavaş hareket, fiyat dağılımı.
/// </summary>
[Trait("Category", "Integration")]
public class CategorySalesReportIntegrationTests : IntegrationTestBase
{
    public CategorySalesReportIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    private IReportManager Sut(out IServiceScope scope)
    {
        var (svc, s) = GetScopedService<IReportManager>();
        scope = s;
        return svc;
    }

    private async Task<int> SeedCategoryAsync(string name)
    {
        using var db = CreateDbContext();
        var cat = new Category { Name = name, CreatedAt = DateTimeOffset.UtcNow };
        db.Set<Category>().Add(cat);
        await db.SaveChangesAsync();
        return cat.Id;
    }

    private async Task SeedSaleAsync(Guid userId, Guid variantId, DateTimeOffset date, decimal unitPrice, int qty)
    {
        using var db = CreateDbContext();
        db.Sales.Add(new Sale
        {
            Id = Guid.NewGuid(),
            SalePersonId = userId,
            BranchOfficeId = 1,
            SaleNumber = $"S-{Guid.NewGuid():N}"[..12],
            SaleDate = date,
            SaleSource = SaleSource.POS,
            SaleStatus = SaleStatus.Completed,
            CreatedAt = DateTimeOffset.UtcNow,
            SaleItems = new List<SaleItem>
            {
                new()
                {
                    Id = Guid.NewGuid(), ProductVariantId = variantId, UnitPrice = unitPrice, Quantity = qty,
                    DiscountPercent = 0, TaxPercentage = 20, Barcode = "BC", ProductTitle = "Ürün",
                    CreatedAt = DateTimeOffset.UtcNow
                }
            }
        });
        await db.SaveChangesAsync();
    }

    private async Task SeedOrderAsync(Guid variantId, DateTimeOffset orderDate, decimal unitPrice, int qty, int? marketPlaceId)
    {
        using var db = CreateDbContext();
        Address Addr() => new() { City = "Ankara", Country = "Türkiye", FullAddress = "Test Adres" };
        db.Set<Order>().Add(new Order
        {
            Id = Guid.NewGuid(),
            MarketPlaceId = marketPlaceId,
            OrderDate = orderDate,
            OrderNumber = $"O-{Guid.NewGuid():N}"[..12],
            TotalQuantity = qty,
            TotalPrice = unitPrice * qty,
            BillingAddress = Addr(),
            ShippingAddress = Addr(),
            CreatedAt = DateTimeOffset.UtcNow,
            OrderItems = new List<OrderItem>
            {
                new() { ProductId = variantId, UnitPrice = unitPrice, Quantity = qty, CreatedAt = DateTimeOffset.UtcNow }
            }
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task CategorySales_Channel_Seasonal_PriceDistribution()
    {
        var start = new DateOnly(2026, 5, 1);
        var end = new DateOnly(2026, 5, 31);
        var may = new DateTimeOffset(2026, 5, 10, 10, 0, 0, TimeSpan.Zero);
        var mayPrevYear = new DateTimeOffset(2025, 5, 10, 10, 0, 0, TimeSpan.Zero);

        await SeedBasicEntitiesAsync();
        var (userId, _) = await SeedCustomerAndUserAsync();
        await SeedMarketPlaceAsync(1, "Trendyol");
        var (_, variantA) = await SeedProductWithStockAsync("CAT-A", 100);

        // Mağaza (Sales): 100 × 2 = 200
        await SeedSaleAsync(userId, variantA, may, 100m, 2);
        // Trendyol (Order, MP=1): 150 × 1 = 150
        await SeedOrderAsync(variantA, may.AddDays(2), 150m, 1, marketPlaceId: 1);
        // Storefront (Order, MP=null): 300 × 1 = 300
        await SeedOrderAsync(variantA, may.AddDays(5), 300m, 1, marketPlaceId: null);
        // Geçen yıl aynı dönem: 100 × 1 = 100
        await SeedSaleAsync(userId, variantA, mayPrevYear, 100m, 1);

        var sut = Sut(out var scope);
        using (scope)
        {
            // --- Kanal karşılaştırma ---
            var channels = await sut.GetCategoryChannelSalesAsync(start, end);
            Assert.Equal(3, channels.Count);
            Assert.Equal(300m, channels.Single(c => c.Channel == "Storefront").Revenue);
            Assert.Equal(200m, channels.Single(c => c.Channel == "Mağaza").Revenue);
            Assert.Equal(150m, channels.Single(c => c.Channel == "Trendyol").Revenue);
            Assert.Equal("Storefront", channels[0].Channel); // ciro desc

            // --- Sezonsal (bu dönem vs geçen yıl) ---
            var seasonal = await sut.GetCategorySeasonalComparisonAsync(start, end);
            var catRow = seasonal.Single(c => c.CategoryName == "Test Kategori");
            Assert.Equal(650m, catRow.CurrentRevenue);      // 200 + 150 + 300
            Assert.Equal(100m, catRow.PreviousYearRevenue);
            Assert.Equal(550.0, catRow.DeltaPercent);

            // --- Fiyat aralığı dağılımı (sadece dönem içi) ---
            var dist = await sut.GetCategoryPriceDistributionAsync(start, end);
            var b100 = dist.Single(b => b.Label == "100 - 250 TL");
            Assert.Equal(2, b100.LineCount);   // 100 ve 150
            Assert.Equal(3, b100.Quantity);    // 2 + 1
            Assert.Equal(350m, b100.Revenue);  // 200 + 150
            var b250 = dist.Single(b => b.Label == "250 - 500 TL");
            Assert.Equal(1, b250.LineCount);
            Assert.Equal(300m, b250.Revenue);
        }
    }

    [Fact]
    public async Task SlowMovingCategories_FlagsStaleOnly()
    {
        await SeedBasicEntitiesAsync();
        var (userId, _) = await SeedCustomerAndUserAsync();
        var (_, variantA) = await SeedProductWithStockAsync("FRESH", 100);
        var catB = await SeedCategoryAsync("Eski Kategori");
        var (_, variantB) = await SeedProductWithStockAsync("STALE", 100, categoryId: catB);

        // A: son satış çok yeni (5 gün önce) → yavaş DEĞİL
        await SeedSaleAsync(userId, variantA, DateTimeOffset.UtcNow.AddDays(-5), 100m, 1);
        // B: son satış 120 gün önce → yavaş
        await SeedSaleAsync(userId, variantB, DateTimeOffset.UtcNow.AddDays(-120), 100m, 1);

        var sut = Sut(out var scope);
        using (scope)
        {
            var slow = await sut.GetSlowMovingCategoriesAsync(staleDays: 30);

            Assert.Contains(slow, c => c.CategoryName == "Eski Kategori");
            Assert.DoesNotContain(slow, c => c.CategoryName == "Test Kategori"); // A'nın kategorisi (fresh)
            var b = slow.Single(c => c.CategoryName == "Eski Kategori");
            Assert.True(b.DaysSinceLastSale >= 100);
        }
    }
}

using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Reports;
using Entegrasyon.Entity.Orders;
using Entegrasyon.Entity.Products;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// Pazaryeri Özeti raporu — gerçek PostgreSQL ile pazaryeri bazında TopSellingProducts
/// join doğrulaması (backlog: MarketplaceSummaryQuery artık en çok satan 3 ürünü döndürür).
///
/// Kritik invariantlar:
///  - TopSellingProducts pazaryeri (o.MarketPlaceId) bazında türetilir, pazaryerleri karışmaz.
///  - Ürünler adede göre AZALAN sıralı, en fazla 3 ile sınırlı.
///  - OrderItem.ProductId (varyant) eşleşiyorsa ürün başlığı (MainProducts.Title); eşleşmiyorsa
///    MerchantSku/Barcode fallback'i kullanılır (eşleşmemiş pazaryeri satırları kaybolmaz).
/// </summary>
[Trait("Category", "Integration")]
public class MarketplaceSummaryReportIntegrationTests : IntegrationTestBase
{
    public MarketplaceSummaryReportIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    private IReportManager Sut(out IServiceScope scope)
    {
        var (svc, s) = GetScopedService<IReportManager>();
        scope = s;
        return svc;
    }

    /// <summary>Product + variant seed (başlığı verilen). Variant Id döner.</summary>
    private async Task<Guid> SeedProductVariantAsync(string title, string barcode)
    {
        using var db = CreateDbContext();
        var brandId = await db.Brands.Select(b => (int?)b.Id).FirstOrDefaultAsync();
        var categoryId = await db.Categories.Select(c => c.Id).FirstAsync();

        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        db.MainProducts.Add(new Product
        {
            Id = productId,
            Title = title,
            BrandId = brandId,
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

    /// <summary>Order + tek OrderItem seed. variantId null ise eşleşmemiş pazaryeri satırı (Barcode/MerchantSku taşır).</summary>
    private async Task SeedOrderAsync(
        int marketPlaceId, Guid? variantId, int quantity, decimal unitPrice,
        string? merchantSku = null, string? barcode = null)
    {
        using var db = CreateDbContext();
        var orderId = Guid.NewGuid();
        db.Orders.Add(new Order
        {
            Id = orderId,
            MarketPlaceId = marketPlaceId,
            OrderNumber = $"ORD-{orderId:N}"[..20],
            CreatedAt = DateTimeOffset.UtcNow,
            OrderItems =
            {
                new OrderItem
                {
                    OrderId = orderId,
                    ProductId = variantId,
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    MerchantSku = merchantSku,
                    Barcode = barcode,
                    CreatedAt = DateTimeOffset.UtcNow
                }
            }
        });
        await db.SaveChangesAsync();
    }

    private static MarketplaceSummaryFilterDto WideRange() => new(
        DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-3)),
        DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(1)));

    // ─── Tests ────────────────────────────────────────────────────────────

    [Fact]
    public async Task TopSellingProducts_OrderedByQuantityDescending_LimitedToThree()
    {
        await SeedBasicEntitiesAsync();
        await SeedMarketPlaceAsync(1, "Trendyol");

        var a = await SeedProductVariantAsync("Ürün A", "BC-A");
        var b = await SeedProductVariantAsync("Ürün B", "BC-B");
        var c = await SeedProductVariantAsync("Ürün C", "BC-C");
        var d = await SeedProductVariantAsync("Ürün D", "BC-D");

        // Adetler: B(30) > A(20) > C(10) > D(5). Top3 = B, A, C.
        await SeedOrderAsync(1, a, quantity: 20, unitPrice: 100);
        await SeedOrderAsync(1, b, quantity: 30, unitPrice: 100);
        await SeedOrderAsync(1, c, quantity: 10, unitPrice: 100);
        await SeedOrderAsync(1, d, quantity: 5, unitPrice: 100);

        var sut = Sut(out var scope);
        using var _ = scope;

        var result = await sut.GetMarketplaceSummaryAsync(WideRange());

        var trendyol = result.Single(r => r.MarketplaceId == 1);
        trendyol.TopSellingProducts.Should().HaveCount(3);
        trendyol.TopSellingProducts.Should().ContainInOrder("Ürün B", "Ürün A", "Ürün C");
        trendyol.TopSellingProducts.Should().NotContain("Ürün D");
    }

    [Fact]
    public async Task TopSellingProducts_AggregatesQuantityAcrossOrders()
    {
        await SeedBasicEntitiesAsync();
        await SeedMarketPlaceAsync(1, "Trendyol");

        var a = await SeedProductVariantAsync("Ürün A", "BC-A");
        var b = await SeedProductVariantAsync("Ürün B", "BC-B");

        // A iki ayrı siparişte 5+5=10; B tek seferde 8. A > B olmalı.
        await SeedOrderAsync(1, a, quantity: 5, unitPrice: 100);
        await SeedOrderAsync(1, a, quantity: 5, unitPrice: 100);
        await SeedOrderAsync(1, b, quantity: 8, unitPrice: 100);

        var sut = Sut(out var scope);
        using var _ = scope;

        var result = await sut.GetMarketplaceSummaryAsync(WideRange());

        var trendyol = result.Single(r => r.MarketplaceId == 1);
        trendyol.TopSellingProducts.Should().ContainInOrder("Ürün A", "Ürün B");
    }

    [Fact]
    public async Task TopSellingProducts_IsolatedPerMarketplace()
    {
        await SeedBasicEntitiesAsync();
        await SeedMarketPlaceAsync(1, "Trendyol");
        await SeedMarketPlaceAsync(2, "Hepsiburada");

        var a = await SeedProductVariantAsync("Ürün A", "BC-A");
        var b = await SeedProductVariantAsync("Ürün B", "BC-B");

        await SeedOrderAsync(1, a, quantity: 50, unitPrice: 100); // sadece Trendyol
        await SeedOrderAsync(2, b, quantity: 7, unitPrice: 100);  // sadece Hepsiburada

        var sut = Sut(out var scope);
        using var _ = scope;

        var result = await sut.GetMarketplaceSummaryAsync(WideRange());

        result.Single(r => r.MarketplaceId == 1).TopSellingProducts.Should().Equal("Ürün A");
        result.Single(r => r.MarketplaceId == 2).TopSellingProducts.Should().Equal("Ürün B");
    }

    [Fact]
    public async Task TopSellingProducts_UnmatchedItem_FallsBackToSkuOrBarcode()
    {
        await SeedBasicEntitiesAsync();
        await SeedMarketPlaceAsync(1, "Trendyol");

        // Eşleşmemiş pazaryeri satırı (ProductId null) — MerchantSku fallback.
        await SeedOrderAsync(1, variantId: null, quantity: 12, unitPrice: 100, merchantSku: "SKU-XYZ", barcode: "BC-XYZ");

        var sut = Sut(out var scope);
        using var _ = scope;

        var result = await sut.GetMarketplaceSummaryAsync(WideRange());

        var trendyol = result.Single(r => r.MarketplaceId == 1);
        trendyol.TopSellingProducts.Should().ContainSingle()
            .Which.Should().Be("SKU-XYZ");
    }
}

using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Matches;
using Entegrasyon.Entity.Products;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// BrandService.GetBrandKpisAsync — Markalar liste sayfası KPI aggregate'leri.
///
/// 3 read-path sayım: TotalProductCount (BrandId != null), MatchedBrandCount (distinct
/// ApplicationBrandId), BrandsWithoutProductCount (NOT EXISTS product). Distinct sayımı,
/// NOT EXISTS korelasyonu ve soft-delete query filter semantiği ancak gerçek Postgres'te
/// doğrulanabilir.
/// </summary>
[Trait("Category", "Integration")]
public class BrandKpiIntegrationTests : IntegrationTestBase
{
    public BrandKpiIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    // ── Seed helpers ────────────────────────────────────────────────────

    private async Task<int> SeedCategoryAsync()
    {
        using var db = CreateDbContext();
        var cat = new Category { Name = "Kategori", CreatedAt = DateTimeOffset.UtcNow };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();
        return cat.Id;
    }

    private async Task<int> SeedBrandAsync(string name)
    {
        using var db = CreateDbContext();
        var brand = new Brand { Name = name, CreatedAt = DateTimeOffset.UtcNow };
        db.Brands.Add(brand);
        await db.SaveChangesAsync();
        return brand.Id;
    }

    private async Task SeedProductAsync(int categoryId, int? brandId, string title, bool isDeleted = false)
    {
        using var db = CreateDbContext();
        db.MainProducts.Add(new Product
        {
            Id = Guid.NewGuid(),
            Title = title,
            CategoryId = categoryId,
            BrandId = brandId,
            IsDeleted = isDeleted,
            DeletedAt = isDeleted ? DateTimeOffset.UtcNow : default,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
    }

    private async Task SeedBrandMatchAsync(int brandId, int marketPlaceId)
    {
        using var db = CreateDbContext();
        db.BrandMarketPlaceMatches.Add(new BrandMarketPlaceMatch
        {
            ApplicationBrandId = brandId,
            MarketPlaceId = marketPlaceId,
            MarketPlaceBrandId = brandId * 1000 + marketPlaceId
        });
        await db.SaveChangesAsync();
    }

    private IBrandService Sut(out IServiceScope scope)
    {
        var (svc, s) = GetScopedService<IBrandService>();
        scope = s;
        return svc;
    }

    // ── Tests ───────────────────────────────────────────────────────────

    [Fact]
    public async Task EmptyDb_ReturnsAllZeros()
    {
        var sut = Sut(out var scope);
        using var _ = scope;

        var result = await sut.GetBrandKpisAsync();

        result.TotalProductCount.Should().Be(0);
        result.MatchedBrandCount.Should().Be(0);
        result.BrandsWithoutProductCount.Should().Be(0);
    }

    [Fact]
    public async Task TotalProductCount_CountsOnlyProductsWithBrand()
    {
        var categoryId = await SeedCategoryAsync();
        var brandId = await SeedBrandAsync("Marka A");

        await SeedProductAsync(categoryId, brandId, "Ürün 1");
        await SeedProductAsync(categoryId, brandId, "Ürün 2");
        await SeedProductAsync(categoryId, brandId: null, "Markasız Ürün"); // hariç
        await SeedProductAsync(categoryId, brandId, "Silinmiş Ürün", isDeleted: true); // hariç (soft-delete)

        var sut = Sut(out var scope);
        using var _ = scope;

        var result = await sut.GetBrandKpisAsync();

        result.TotalProductCount.Should().Be(2); // sadece markalı + silinmemiş
    }

    [Fact]
    public async Task MatchedBrandCount_CountsDistinctBrandsWithAtLeastOneMatch()
    {
        var matchedBrand = await SeedBrandAsync("Eşleşmiş Marka");
        var multiMatchBrand = await SeedBrandAsync("Çok Eşleşmeli Marka");
        await SeedBrandAsync("Eşleşmesiz Marka"); // 0 eşleşme → sayılmaz

        await SeedMarketPlaceAsync(1, "Trendyol");
        await SeedMarketPlaceAsync(2, "Hepsiburada");

        await SeedBrandMatchAsync(matchedBrand, 1);
        // Aynı marka 2 pazaryeri eşleşmesi → distinct sayımda 1 olmalı
        await SeedBrandMatchAsync(multiMatchBrand, 1);
        await SeedBrandMatchAsync(multiMatchBrand, 2);

        var sut = Sut(out var scope);
        using var _ = scope;

        var result = await sut.GetBrandKpisAsync();

        result.MatchedBrandCount.Should().Be(2); // 2 distinct marka (3. eşleşme aynı markada)
    }

    [Fact]
    public async Task BrandsWithoutProductCount_CountsBrandsWithZeroProducts()
    {
        var categoryId = await SeedCategoryAsync();
        var brandWithProduct = await SeedBrandAsync("Ürünlü Marka");
        await SeedBrandAsync("Ürünsüz Marka 1"); // 0 ürün
        await SeedBrandAsync("Ürünsüz Marka 2"); // 0 ürün

        await SeedProductAsync(categoryId, brandWithProduct, "Ürün");

        var sut = Sut(out var scope);
        using var _ = scope;

        var result = await sut.GetBrandKpisAsync();

        result.BrandsWithoutProductCount.Should().Be(2);
    }

    [Fact]
    public async Task BrandsWithoutProductCount_SoftDeletedProductMakesBrandCountAsEmpty()
    {
        var categoryId = await SeedCategoryAsync();
        var brandId = await SeedBrandAsync("Sadece Silinmiş Ürünlü Marka");
        await SeedProductAsync(categoryId, brandId, "Silinmiş Ürün", isDeleted: true);

        var sut = Sut(out var scope);
        using var _ = scope;

        var result = await sut.GetBrandKpisAsync();

        // Markanın tek ürünü soft-deleted → query filter onu görmez → marka "ürünsüz" sayılır
        result.BrandsWithoutProductCount.Should().Be(1);
        result.TotalProductCount.Should().Be(0);
    }
}

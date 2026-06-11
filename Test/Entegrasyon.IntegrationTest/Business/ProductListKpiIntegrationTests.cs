using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Tenants;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Products;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// GetProductListKpiAsync integration testleri — gerçek PostgreSQL ile KPI aggregate matematiği.
/// Stok kaynağı liste tablosu (TotalCurrentStock = SUM(FirstTotalStock - SoldQuantity)) ile
/// birebir tutarlı olmalı; düşük-stok eşiği liste badge'i (stok 1..4) ile aynı olmalı.
/// </summary>
[Trait("Category", "Integration")]
public class ProductListKpiIntegrationTests : IntegrationTestBase
{
    public ProductListKpiIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    protected override async Task OnInitializeAsync()
    {
        using var dbContext = CreateDbContext();
        if (!await dbContext.BranchOffices.AnyAsync(b => b.Id == 1))
            dbContext.BranchOffices.Add(new BranchOffice
            {
                Id = 1, Name = "Ana Depo", IsDefaultMarketPlaceStock = true,
                CreatedAt = DateTimeOffset.UtcNow
            });
        if (!await dbContext.Brands.AnyAsync(b => b.Name == "KPI Marka"))
            dbContext.Brands.Add(new Brand { Name = "KPI Marka", CreatedAt = DateTimeOffset.UtcNow });
        if (!await dbContext.Categories.AnyAsync(c => c.Name == "KPI Kategori"))
            dbContext.Categories.Add(new Category { Name = "KPI Kategori", CreatedAt = DateTimeOffset.UtcNow });
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// IProductService'i tenant context initialize edilmiş bir scope'tan çözer.
    /// TenantMemoryCache TenantId'ye eriştiğinden (cache key prefix) service-level testte
    /// tenant context init edilmeli — middleware yok. Polling service deseniyle aynı.
    /// </summary>
    private (IProductService Service, IServiceScope Scope) GetTenantScopedProductService()
    {
        var scope = Services.CreateScope();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        if (!tenantContext.IsInitialized)
            tenantContext.Initialize(new TenantRegistryEntry(
                TenantId: 1, Subdomain: "test", CompanyName: "Test",
                ConnectionString: "test", IsActive: true, LicenseType: null));
        return (scope.ServiceProvider.GetRequiredService<IProductService>(), scope);
    }

    /// <summary>
    /// Tek varyantlı ürün, branch stock FirstTotalStock=stock & SoldQuantity=sold seed eder.
    /// CurrentStock (computed) = stock - sold.
    /// </summary>
    private async Task SeedProductAsync(string barcode, int firstTotalStock, int soldQuantity = 0, bool deleted = false)
    {
        using var dbContext = CreateDbContext();
        var brandId = await dbContext.Brands.Where(b => b.Name == "KPI Marka").Select(b => b.Id).FirstAsync();
        var categoryId = await dbContext.Categories.Where(c => c.Name == "KPI Kategori").Select(c => c.Id).FirstAsync();

        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        dbContext.MainProducts.Add(new Product
        {
            Id = productId, Title = $"KPI {barcode}", Description = "", StockCode = $"KPI-{barcode}",
            BrandId = brandId, CategoryId = categoryId, IsDeleted = deleted,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.ProductVariants.Add(new ProductVariant
        {
            Id = variantId, ProductId = productId, Barcode = barcode,
            ListPrice = 200, SalePrice = 180, CurrencyType = "TRY",
            IsDeleted = deleted,
            CreatedAt = DateTimeOffset.UtcNow
        });
        dbContext.BranchOfficeStocks.Add(new BranchOfficeStock
        {
            BranchOfficeId = 1, ProductVariantId = variantId,
            FirstTotalStock = firstTotalStock, SoldQuantity = soldQuantity
        });
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetProductListKpi_EmptyTenant_ReturnsAllZero()
    {
        var (svc, scope) = GetTenantScopedProductService();
        using var _ = scope;

        var kpi = await svc.GetProductListKpiAsync();

        kpi.TotalStock.Should().Be(0);
        kpi.LowStockCount.Should().Be(0);
        kpi.TotalVariantCount.Should().Be(0);
    }

    [Fact]
    public async Task GetProductListKpi_SumsCurrentStock_ConsistentWithListTable()
    {
        // Ürün A: 10 - 3 = 7 mevcut; Ürün B: 20 - 0 = 20 mevcut → toplam 27
        await SeedProductAsync("KPI-STOCK-A", firstTotalStock: 10, soldQuantity: 3);
        await SeedProductAsync("KPI-STOCK-B", firstTotalStock: 20, soldQuantity: 0);

        var (svc, scope) = GetTenantScopedProductService();
        using var _ = scope;
        var kpi = await svc.GetProductListKpiAsync();

        kpi.TotalStock.Should().Be(27);
        kpi.TotalVariantCount.Should().Be(2);
    }

    [Theory]
    // Eşik sınırları — liste badge'i ile birebir: "Düşük" = stok 1..4
    [InlineData(0, false)]  // Tükendi — düşük DEĞİL
    [InlineData(1, true)]   // alt sınır — düşük
    [InlineData(4, true)]   // üst sınır — düşük
    [InlineData(5, false)]  // Yeterli — düşük DEĞİL (eşik dışı)
    [InlineData(9, false)]  // Yeterli
    public async Task GetProductListKpi_LowStockThreshold_MatchesListBadge(int currentStock, bool isLow)
    {
        await SeedProductAsync($"KPI-THR-{currentStock}", firstTotalStock: currentStock, soldQuantity: 0);

        var (svc, scope) = GetTenantScopedProductService();
        using var _ = scope;
        var kpi = await svc.GetProductListKpiAsync();

        kpi.LowStockCount.Should().Be(isLow ? 1 : 0);
    }

    [Fact]
    public async Task GetProductListKpi_ExcludesDeletedProducts()
    {
        await SeedProductAsync("KPI-LIVE", firstTotalStock: 8, soldQuantity: 0);
        await SeedProductAsync("KPI-DELETED", firstTotalStock: 100, soldQuantity: 0, deleted: true);

        var (svc, scope) = GetTenantScopedProductService();
        using var _ = scope;
        var kpi = await svc.GetProductListKpiAsync();

        // Silinmiş ürün stoğa/varyant sayısına KARIŞMAMALI (liste query-filter ile aynı)
        kpi.TotalStock.Should().Be(8);
        kpi.TotalVariantCount.Should().Be(1);
        kpi.LowStockCount.Should().Be(0);
    }

    [Fact]
    public async Task GetProductListKpi_SecondCall_ServedFromCache_NotRecomputed()
    {
        await SeedProductAsync("KPI-CACHE", firstTotalStock: 10, soldQuantity: 0);

        // İlk çağrı — DB'den hesaplanır ve cache'lenir. AYNI scope (aynı tenant) ile devam.
        var (svc, scope) = GetTenantScopedProductService();
        using var _ = scope;
        var first = await svc.GetProductListKpiAsync();
        first.TotalStock.Should().Be(10);

        // DB'yi arkadan değiştir — cache hâlâ eski değeri dönmeli (ikinci çağrı DB'ye gitmedi).
        await SeedProductAsync("KPI-CACHE-2", firstTotalStock: 50, soldQuantity: 0);

        var second = await svc.GetProductListKpiAsync();
        second.TotalStock.Should().Be(10, "ikinci çağrı kısa-TTL cache'ten gelmeli, DB'ye gitmemeli");
        second.TotalVariantCount.Should().Be(1, "varyant sayısı da cache'ten gelmeli");
    }
}

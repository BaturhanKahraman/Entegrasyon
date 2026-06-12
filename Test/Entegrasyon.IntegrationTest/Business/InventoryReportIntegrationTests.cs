using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Reports;
using Entegrasyon.Entity.Products;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// Envanter raporu (/reports/inventory) — gerçek PostgreSQL ile esnafın 4 sorusunun DB-tarafı:
///  - Stok Değeri (TL) = ProductVariant.CostPrice × CurrentStock (satır + KPI özet toplam).
///  - LastStockEntryDate = varyant+şube başına son pozitif stok hareketinin tarihi (ölü stok tespiti).
///  - Dönem SoldQuantity = dönemdeki satış hareketlerinden (Sale/MarketplaceSale) türetilir.
///  - UnsoldOnly = dönemde satılmayan (SoldQuantity==0) satırları filtreler.
///
/// Tüm türetimler tek grouped aggregate (N+1 yok), hot StockMovements tablosu →
/// (BranchOfficeId, ProductVariantId, CreatedAt DESC) WHERE NOT IsDeleted index'i ile karşılanır.
/// </summary>
[Trait("Category", "Integration")]
public class InventoryReportIntegrationTests : IntegrationTestBase
{
    public InventoryReportIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    private async Task SeedBranchAsync(int id, string name)
    {
        using var db = CreateDbContext();
        if (!await db.BranchOffices.AnyAsync(b => b.Id == id))
        {
            db.BranchOffices.Add(new BranchOffice { Id = id, Name = name, CreatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
        }
    }

    private async Task<Guid> SeedStockAsync(
        int branchOfficeId, int firstTotalStock, int soldQuantity, string barcode,
        decimal costPrice, string title = "Ürün")
    {
        using var db = CreateDbContext();
        var brandId = await db.Brands.Select(b => (int?)b.Id).FirstOrDefaultAsync();
        var categoryId = await db.Categories.Select(c => (int?)c.Id).FirstOrDefaultAsync();

        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        db.MainProducts.Add(new Product
        {
            Id = productId,
            Title = title,
            BrandId = brandId,
            CategoryId = categoryId!.Value,
            CreatedAt = DateTimeOffset.UtcNow
        });
        db.ProductVariants.Add(new ProductVariant
        {
            Id = variantId,
            ProductId = productId,
            Barcode = barcode,
            CostPrice = costPrice,
            CurrencyType = "TRY",
            CreatedAt = DateTimeOffset.UtcNow
        });
        db.BranchOfficeStocks.Add(new BranchOfficeStock
        {
            BranchOfficeId = branchOfficeId,
            ProductVariantId = variantId,
            FirstTotalStock = firstTotalStock,
            SoldQuantity = soldQuantity
        });
        await db.SaveChangesAsync();
        return variantId;
    }

    private async Task SeedMovementAsync(
        int branchOfficeId, Guid variantId, StockMovementType type, int quantity, DateTimeOffset createdAt)
    {
        using var db = CreateDbContext();
        var mv = new StockMovement
        {
            BranchOfficeId = branchOfficeId,
            ProductVariantId = variantId,
            Type = type,
            Quantity = quantity,
            StockBefore = 0,
            StockAfter = quantity
        };
        db.Set<StockMovement>().Add(mv);
        await db.SaveChangesAsync();

        // SaveChangesAsync CreatedAt'i UtcNow'a ezer → ayrık tarihler için ExecuteUpdate ile zorla.
        await db.Set<StockMovement>()
            .Where(m => m.Id == mv.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.CreatedAt, createdAt));
    }

    private IReportManager Sut(out IServiceScope scope)
    {
        var (svc, s) = GetScopedService<IReportManager>();
        scope = s;
        return svc;
    }

    [Fact]
    public async Task StockValue_ComputedFromCostPriceTimesStock()
    {
        await SeedBasicEntitiesAsync();
        await SeedBranchAsync(1, "Ana Depo");
        await SeedStockAsync(1, firstTotalStock: 10, soldQuantity: 0, barcode: "BC-VAL", costPrice: 12.50m);

        var sut = Sut(out var scope);
        using var _ = scope;

        var report = await sut.GetInventoryReportAsync(new InventoryReportFilterDto(null, StockFilter.All));

        var row = report.Items.Single(i => i.Barcode == "BC-VAL");
        row.StockValue.Should().Be(125.00m);          // 12.50 × 10
        report.Summary.StockValue.Should().Be(125.00m);
    }

    [Fact]
    public async Task LastStockEntryDate_DerivedFromLatestPositiveMovement()
    {
        await SeedBasicEntitiesAsync();
        await SeedBranchAsync(1, "Ana Depo");
        var variant = await SeedStockAsync(1, 5, 0, "BC-ENTRY", costPrice: 5m);
        var older = DateTimeOffset.UtcNow.AddDays(-20);
        var newer = DateTimeOffset.UtcNow.AddDays(-3);
        await SeedMovementAsync(1, variant, StockMovementType.InitialStock, 50, older);
        await SeedMovementAsync(1, variant, StockMovementType.Return, 5, newer);

        var sut = Sut(out var scope);
        using var _ = scope;

        var report = await sut.GetInventoryReportAsync(new InventoryReportFilterDto(null, StockFilter.All));

        var row = report.Items.Single(i => i.Barcode == "BC-ENTRY");
        row.LastStockEntryDate.Should().Be(DateOnly.FromDateTime(newer.UtcDateTime));
    }

    [Fact]
    public async Task PeriodSoldQuantity_DerivedFromSaleMovementsInRange()
    {
        await SeedBasicEntitiesAsync();
        await SeedBranchAsync(1, "Ana Depo");
        var variant = await SeedStockAsync(1, 100, 999, "BC-PERIOD", costPrice: 1m);
        // Dönem içi 2 satış (-3, -2 adet) = 5; dönem dışı 1 satış (-7) sayılmamalı.
        await SeedMovementAsync(1, variant, StockMovementType.Sale, -3, DateTimeOffset.UtcNow.AddDays(-5));
        await SeedMovementAsync(1, variant, StockMovementType.MarketplaceSale, -2, DateTimeOffset.UtcNow.AddDays(-4));
        await SeedMovementAsync(1, variant, StockMovementType.Sale, -7, DateTimeOffset.UtcNow.AddDays(-40));

        var sut = Sut(out var scope);
        using var _ = scope;

        var start = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10));
        var end = DateOnly.FromDateTime(DateTime.UtcNow);
        var report = await sut.GetInventoryReportAsync(
            new InventoryReportFilterDto(null, StockFilter.All, start, end));

        var row = report.Items.Single(i => i.Barcode == "BC-PERIOD");
        row.SoldQuantity.Should().Be(5);   // dönem içi |−3| + |−2|, kümülatif 999 değil
    }

    [Fact]
    public async Task UnsoldOnly_KeepsOnlyZeroSoldInPeriod()
    {
        await SeedBasicEntitiesAsync();
        await SeedBranchAsync(1, "Ana Depo");
        var sold = await SeedStockAsync(1, 50, 0, "BC-SOLD", costPrice: 1m);
        await SeedStockAsync(1, 50, 0, "BC-UNSOLD", costPrice: 1m);
        await SeedMovementAsync(1, sold, StockMovementType.Sale, -4, DateTimeOffset.UtcNow.AddDays(-2));

        var sut = Sut(out var scope);
        using var _ = scope;

        var start = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10));
        var end = DateOnly.FromDateTime(DateTime.UtcNow);
        var report = await sut.GetInventoryReportAsync(
            new InventoryReportFilterDto(null, StockFilter.All, start, end, UnsoldOnly: true));

        report.Items.Should().Contain(i => i.Barcode == "BC-UNSOLD");
        report.Items.Should().NotContain(i => i.Barcode == "BC-SOLD");
    }
}

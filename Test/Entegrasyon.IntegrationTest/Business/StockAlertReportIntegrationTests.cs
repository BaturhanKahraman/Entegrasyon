using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Reports;
using Entegrasyon.Entity.Products;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// Stok Alert raporu — gerçek PostgreSQL ile branch join, seviye türetimi, KPI özet
/// ve filtre-öncesi-pagination doğrulaması.
///
/// Kritik invariantlar:
///  - Filtreler (BranchOfficeId, AlertLevel) pagination'dan ÖNCE uygulanır → sayfa sayıları doğru.
///  - KPI özet TÜM filtrelenmiş küme üzerinden (sayfa değil) sayılır.
///  - LastStockEntryDate StockMovements'tan türetilir (pozitif/initial hareket max CreatedAt).
///  - AlertLevel: CurrentStock<=0 veya DaysUntilStockout<=3 → Critical; CurrentStock<=Minimum → Low.
/// </summary>
[Trait("Category", "Integration")]
public class StockAlertReportIntegrationTests : IntegrationTestBase
{
    public StockAlertReportIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    private async Task SeedBranchAsync(int id, string name)
    {
        using var db = CreateDbContext();
        if (!await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .AnyAsync(db.BranchOffices, b => b.Id == id))
        {
            db.BranchOffices.Add(new BranchOffice
            {
                Id = id,
                Name = name,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }
    }

    /// <summary>Product + variant + BranchOfficeStock (FirstTotalStock/SoldQuantity) seed. CurrentStock = First - Sold (computed).</summary>
    private async Task<Guid> SeedStockAsync(
        int branchOfficeId, int firstTotalStock, int soldQuantity, string barcode, string title = "Ürün")
    {
        using var db = CreateDbContext();
        var brandId = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .FirstOrDefaultAsync(System.Linq.Queryable.Select(db.Brands, b => (int?)b.Id));
        var categoryId = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .FirstOrDefaultAsync(System.Linq.Queryable.Select(db.Categories, c => (int?)c.Id));

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

    private async Task SeedStockMovementAsync(int branchOfficeId, Guid variantId, int quantity, DateTimeOffset createdAt)
    {
        using var db = CreateDbContext();
        var mv = new StockMovement
        {
            BranchOfficeId = branchOfficeId,
            ProductVariantId = variantId,
            Type = StockMovementType.InitialStock,
            Quantity = quantity,
            StockBefore = 0,
            StockAfter = quantity
        };
        db.Set<StockMovement>().Add(mv);
        await db.SaveChangesAsync();

        // SaveChangesAsync CreatedAt'i UtcNow'a EZER → testte ayrık tarihler için ExecuteUpdate ile zorla.
        await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ExecuteUpdateAsync(
                System.Linq.Queryable.Where(db.Set<StockMovement>(), m => m.Id == mv.Id),
                s => s.SetProperty(m => m.CreatedAt, createdAt));
    }

    private IReportManager Sut(out IServiceScope scope)
    {
        var (svc, s) = GetScopedService<IReportManager>();
        scope = s;
        return svc;
    }

    // ─── Tests ────────────────────────────────────────────────────────────

    [Fact]
    public async Task BelowThresholdRows_ReturnedWithBranchInfo()
    {
        await SeedBasicEntitiesAsync();
        await SeedBranchAsync(1, "Ana Depo");
        await SeedStockAsync(1, firstTotalStock: 3, soldQuantity: 0, barcode: "BC-LOW-1"); // CurrentStock 3
        await SeedStockAsync(1, firstTotalStock: 100, soldQuantity: 0, barcode: "BC-OK-1"); // CurrentStock 100 — eşik üstü

        var sut = Sut(out var scope);
        using var _ = scope;

        var report = await sut.GetStockAlertReportAsync(new StockAlertPaginatedRequest
        {
            MinimumStockThreshold = 10,
            PageIndex = 0,
            PageSize = 50
        });

        report.Items.Items.Should().ContainSingle();
        var row = report.Items.Items[0];
        row.Barcode.Should().Be("BC-LOW-1");
        row.BranchOfficeId.Should().Be(1);
        row.BranchOfficeName.Should().Be("Ana Depo");
    }

    [Fact]
    public async Task AlertLevel_DerivedFromStock()
    {
        await SeedBasicEntitiesAsync();
        await SeedBranchAsync(1, "Ana Depo");
        await SeedStockAsync(1, firstTotalStock: 0, soldQuantity: 0, barcode: "BC-OUT");   // CurrentStock 0 → Critical
        await SeedStockAsync(1, firstTotalStock: 8, soldQuantity: 0, barcode: "BC-LOW");   // 8 <= 10 minimum → Low

        var sut = Sut(out var scope);
        using var _ = scope;

        var report = await sut.GetStockAlertReportAsync(new StockAlertPaginatedRequest
        {
            MinimumStockThreshold = 10, PageIndex = 0, PageSize = 50
        });

        var outRow = report.Items.Items.Single(r => r.Barcode == "BC-OUT");
        outRow.AlertLevel.Should().Be(StockAlertLevel.Critical);

        var lowRow = report.Items.Items.Single(r => r.Barcode == "BC-LOW");
        lowRow.AlertLevel.Should().Be(StockAlertLevel.Low);
    }

    [Fact]
    public async Task BranchFilter_AppliedBeforePagination()
    {
        await SeedBasicEntitiesAsync();
        await SeedBranchAsync(1, "Ana Depo");
        await SeedBranchAsync(2, "Şube 2");
        await SeedStockAsync(1, 3, 0, "BC-B1-A");
        await SeedStockAsync(1, 4, 0, "BC-B1-B");
        await SeedStockAsync(2, 2, 0, "BC-B2-A");

        var sut = Sut(out var scope);
        using var _ = scope;

        var report = await sut.GetStockAlertReportAsync(new StockAlertPaginatedRequest
        {
            MinimumStockThreshold = 10, BranchOfficeId = 2, PageIndex = 0, PageSize = 1
        });

        // Sadece şube 2 → totalCount 1 (pagination öncesi filtre)
        report.Items.TotalItemCount.Should().Be(1);
        report.Items.Items.Should().ContainSingle();
        report.Items.Items[0].Barcode.Should().Be("BC-B2-A");
    }

    [Fact]
    public async Task AlertLevelFilter_OnlyCritical()
    {
        await SeedBasicEntitiesAsync();
        await SeedBranchAsync(1, "Ana Depo");
        await SeedStockAsync(1, 0, 0, "BC-CRIT");  // CurrentStock 0 → Critical
        await SeedStockAsync(1, 8, 0, "BC-LOW");   // Low

        var sut = Sut(out var scope);
        using var _ = scope;

        var report = await sut.GetStockAlertReportAsync(new StockAlertPaginatedRequest
        {
            MinimumStockThreshold = 10, AlertLevel = StockAlertLevel.Critical, PageIndex = 0, PageSize = 50
        });

        report.Items.TotalItemCount.Should().Be(1);
        report.Items.Items.Should().ContainSingle()
            .Which.Barcode.Should().Be("BC-CRIT");
    }

    [Fact]
    public async Task Summary_CountsOverFullSet_NotJustPage()
    {
        await SeedBasicEntitiesAsync();
        await SeedBranchAsync(1, "Ana Depo");
        // 2 tükendi (CurrentStock 0 → Critical+OutOfStock), 1 kritik (DaysUntilStockout), 3 düşük
        await SeedStockAsync(1, 0, 0, "BC-OUT-1");
        await SeedStockAsync(1, 0, 0, "BC-OUT-2");
        await SeedStockAsync(1, 9, 0, "BC-LOW-1");
        await SeedStockAsync(1, 8, 0, "BC-LOW-2");
        await SeedStockAsync(1, 7, 0, "BC-LOW-3");

        var sut = Sut(out var scope);
        using var _ = scope;

        // PageSize 1 → sayfa sadece 1 satır içerir; özet yine tüm kümeyi saymalı
        var report = await sut.GetStockAlertReportAsync(new StockAlertPaginatedRequest
        {
            MinimumStockThreshold = 10, PageIndex = 0, PageSize = 1
        });

        report.Items.Items.Should().ContainSingle();             // sayfa 1 satır
        report.Summary.OutOfStockCount.Should().Be(2);           // tüm küme
        report.Summary.LowCount.Should().Be(3);
        // Critical: CurrentStock<=0 olan 2 satır
        report.Summary.CriticalCount.Should().Be(2);
        report.Summary.TotalAlerts.Should().Be(5);
    }

    [Fact]
    public async Task LastStockEntryDate_DerivedFromStockMovements()
    {
        await SeedBasicEntitiesAsync();
        await SeedBranchAsync(1, "Ana Depo");
        var variant = await SeedStockAsync(1, 3, 0, "BC-ENTRY");
        var older = DateTimeOffset.UtcNow.AddDays(-10);
        var newer = DateTimeOffset.UtcNow.AddDays(-2);
        await SeedStockMovementAsync(1, variant, 50, older);
        await SeedStockMovementAsync(1, variant, 10, newer);

        var sut = Sut(out var scope);
        using var _ = scope;

        var report = await sut.GetStockAlertReportAsync(new StockAlertPaginatedRequest
        {
            MinimumStockThreshold = 10, PageIndex = 0, PageSize = 50
        });

        var row = report.Items.Items.Single(r => r.Barcode == "BC-ENTRY");
        row.LastStockEntryDate.Should().NotBeNull();
        row.LastStockEntryDate!.Value.Should().BeCloseTo(newer.UtcDateTime, TimeSpan.FromSeconds(2));
    }
}

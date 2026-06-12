using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Sales;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// İade Raporu analitiği (SaleReturn temelli) — gerçek PostgreSQL.
///
/// Kapsam:
///  - Nedensel trend: aylık + neden bazlı seriler; boş ay 0.
///  - Ürün bazlı iade oranı: iade adedi / satılan adet; minSold + alert eşiği.
///  - İade maliyeti: gerçekleşen iade (Approved/Completed) refund + tahmini kargo/süreç.
/// Rejected/dönem-dışı iadeler HARİÇ.
/// </summary>
[Trait("Category", "Integration")]
public class ReturnsReportIntegrationTests : IntegrationTestBase
{
    public ReturnsReportIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    private IReportManager Sut(out IServiceScope scope)
    {
        var (svc, s) = GetScopedService<IReportManager>();
        scope = s;
        return svc;
    }

    private async Task<Guid> SeedSaleWithItemAsync(
        Guid userId, Guid variantId, DateTimeOffset saleDate, decimal unitPrice, int qty)
    {
        using var db = CreateDbContext();
        var saleId = Guid.NewGuid();
        var item = new SaleItem
        {
            Id = Guid.NewGuid(),
            ProductVariantId = variantId,
            UnitPrice = unitPrice,
            Quantity = qty,
            DiscountPercent = 0,
            TaxPercentage = 20,
            Barcode = "BC-A",
            ProductTitle = "İade Test Ürünü",
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Sales.Add(new Sale
        {
            Id = saleId,
            SalePersonId = userId,
            BranchOfficeId = 1,
            SaleNumber = $"S-{Guid.NewGuid():N}"[..12],
            SaleDate = saleDate,
            SaleSource = SaleSource.POS,
            SaleStatus = SaleStatus.Completed,
            CreatedAt = DateTimeOffset.UtcNow,
            SaleItems = new List<SaleItem> { item }
        });
        await db.SaveChangesAsync();
        return saleId;
    }

    private async Task<int> SeedReturnReasonAsync(string code, string name)
    {
        using var db = CreateDbContext();
        var reason = new ReturnReason { Code = code, Name = name, IsActive = true, CreatedAt = DateTimeOffset.UtcNow };
        db.Set<ReturnReason>().Add(reason);
        await db.SaveChangesAsync();
        return reason.Id;
    }

    private async Task SeedReturnAsync(
        Guid saleId, Guid variantId, ReturnStatus status, DateTimeOffset returnDate,
        int qty, decimal refund, int? reasonId = null, string? customReason = null)
    {
        using var db = CreateDbContext();
        // Bu varyanta ait herhangi bir SaleItem'i bul (iade kalemi bağlamak için).
        var saleItemId = db.Set<SaleItem>().First(si => si.ProductVariantId == variantId).Id;

        db.Set<SaleReturn>().Add(new SaleReturn
        {
            SaleId = saleId,
            Source = ReturnSource.InPerson,
            ReturnDate = returnDate,
            ReturnStatus = status,
            ReturnReasonId = reasonId,
            CustomReason = customReason,
            RefundAmount = refund,
            CreatedAt = DateTimeOffset.UtcNow,
            Items = new List<SaleReturnItem>
            {
                new() { SaleItemId = saleItemId, Quantity = qty, CreatedAt = DateTimeOffset.UtcNow }
            }
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task ReturnsAnalytics_Trend_ProductRate_And_Cost()
    {
        var start = new DateOnly(2026, 4, 1);
        var end = new DateOnly(2026, 6, 30);
        var may = new DateTimeOffset(2026, 5, 10, 10, 0, 0, TimeSpan.Zero);
        var june = new DateTimeOffset(2026, 6, 12, 10, 0, 0, TimeSpan.Zero);
        var outRange = new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);

        await SeedBasicEntitiesAsync();
        var (userId, _) = await SeedCustomerAndUserAsync();
        var (_, variantId) = await SeedProductWithStockAsync("RET-A", 100);

        // Satış: 10 adet (sold)
        var saleId = await SeedSaleWithItemAsync(userId, variantId, may, 100m, 10);

        var bedenId = await SeedReturnReasonAsync("SIZE", "Beden Uyumsuz");

        // Gerçekleşen iadeler
        await SeedReturnAsync(saleId, variantId, ReturnStatus.Approved, may, qty: 3, refund: 200m, reasonId: bedenId);
        await SeedReturnAsync(saleId, variantId, ReturnStatus.Completed, june, qty: 1, refund: 100m, customReason: "Hasarlı");
        // Hariç olması gerekenler
        await SeedReturnAsync(saleId, variantId, ReturnStatus.Rejected, may, qty: 5, refund: 999m, reasonId: bedenId);   // Rejected
        await SeedReturnAsync(saleId, variantId, ReturnStatus.Approved, outRange, qty: 5, refund: 999m, reasonId: bedenId); // dönem dışı

        var sut = Sut(out var scope);
        using (scope)
        {
            // --- Nedensel trend ---
            var trend = await sut.GetReturnReasonTrendAsync(start, end);
            Assert.Equal(new[] { "04.2026", "05.2026", "06.2026" }, trend.Months);
            Assert.Equal(2, trend.Series.Count);

            var beden = trend.Series.Single(s => s.Reason == "Beden Uyumsuz");
            Assert.Equal(new[] { 0, 1, 0 }, beden.Counts);
            var hasarli = trend.Series.Single(s => s.Reason == "Hasarlı");
            Assert.Equal(new[] { 0, 0, 1 }, hasarli.Counts);

            // --- Ürün bazlı iade oranı ---
            var rates = await sut.GetProductReturnRatesAsync(start, end, minSold: 5, alertThresholdPercent: 10);
            var prod = Assert.Single(rates);
            Assert.Equal(variantId, prod.ProductVariantId);
            Assert.Equal(10, prod.SoldQuantity);
            Assert.Equal(4, prod.ReturnedQuantity);          // 3 + 1
            Assert.Equal(40.0, prod.ReturnRatePercent);
            Assert.True(prod.IsAlert);

            // --- İade maliyeti ---
            var cost = await sut.GetReturnCostAsync(start, end, shippingPerReturn: 50m, processPerReturn: 25m);
            Assert.Equal(2, cost.ReturnCount);
            Assert.Equal(300m, cost.TotalRefund);
            Assert.Equal(100m, cost.EstimatedShippingCost);  // 2 * 50
            Assert.Equal(50m, cost.EstimatedProcessCost);    // 2 * 25
            Assert.Equal(450m, cost.TotalCost);
            Assert.Equal(225m, cost.AverageCostPerReturn);
        }
    }
}

using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Invoicing;
using Entegrasyon.Entity.Sales;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// Fatura Türü Dağılımı (E-Fatura / E-Arşiv / Faturasız) — gerçek PostgreSQL ile.
///
/// Kritik invariantlar:
///  - E-Fatura/E-Arşiv: sadece KESİLEN faturalar (Sent/Accepted), InvoiceType'a göre adet + GrandTotal.
///  - Faturasız: dönem içi iptal-olmayan satışlardan faturası kesilmemiş olanlar.
///  - Faturası kesilmiş satış (EInvoice.SaleId eşleşmesi) faturasıza GİRMEZ.
///  - Dönem dışı / iptal satış HARİÇ; Draft fatura HARİÇ.
/// </summary>
[Trait("Category", "Integration")]
public class InvoiceTypeBreakdownReportIntegrationTests : IntegrationTestBase
{
    public InvoiceTypeBreakdownReportIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    private IReportManager Sut(out IServiceScope scope)
    {
        var (svc, s) = GetScopedService<IReportManager>();
        scope = s;
        return svc;
    }

    private async Task SeedInvoiceAsync(
        EInvoiceType type, EInvoiceStatus status, DateTimeOffset issueDate, decimal grandTotal, Guid? saleId = null)
    {
        using var db = CreateDbContext();
        db.Set<EInvoice>().Add(new EInvoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = Guid.NewGuid().ToString("N")[..10],
            InvoiceType = type,
            Status = status,
            IssueDate = issueDate,
            GrandTotal = grandTotal,
            SaleId = saleId,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
    }

    private async Task<Guid> SeedSaleAsync(
        Guid userId, Guid variantId, DateTimeOffset saleDate,
        decimal unitPrice, int qty, SaleStatus status = SaleStatus.Completed)
    {
        using var db = CreateDbContext();
        var saleId = Guid.NewGuid();
        db.Sales.Add(new Sale
        {
            Id = saleId,
            SalePersonId = userId,
            BranchOfficeId = 1,
            SaleNumber = $"S-{Guid.NewGuid():N}"[..12],
            SaleDate = saleDate,
            SaleSource = SaleSource.POS,
            SaleStatus = status,
            CreatedAt = DateTimeOffset.UtcNow,
            SaleItems = new List<SaleItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductVariantId = variantId,
                    UnitPrice = unitPrice,
                    Quantity = qty,
                    DiscountPercent = 0,
                    TaxPercentage = 20,
                    Barcode = "BC",
                    ProductTitle = "Ürün",
                    CreatedAt = DateTimeOffset.UtcNow
                }
            }
        });
        await db.SaveChangesAsync();
        return saleId;
    }

    [Fact]
    public async Task SplitsInvoicedAndUninvoiced_ExcludingDraftCancelledAndOutOfRange()
    {
        var start = new DateOnly(2026, 4, 1);
        var end = new DateOnly(2026, 6, 30);
        var inRange = new DateTimeOffset(2026, 5, 10, 10, 0, 0, TimeSpan.Zero);
        var outRange = new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);

        await SeedBasicEntitiesAsync();
        var (userId, _) = await SeedCustomerAndUserAsync();
        var (_, variantId) = await SeedProductWithStockAsync("FT-BREAKDOWN", 100);

        // Faturalı satış (Sent E-Fatura ile bağlı) → faturasıza GİRMEZ, E-Fatura'ya sayılır.
        var invoicedSaleId = await SeedSaleAsync(userId, variantId, inRange, 100m, 3); // 300

        // E-Fatura: bağımsız (120) + satış-bağlı (300) = 2 adet / 420
        await SeedInvoiceAsync(EInvoiceType.EFatura, EInvoiceStatus.Sent, inRange, 120m);
        await SeedInvoiceAsync(EInvoiceType.EFatura, EInvoiceStatus.Accepted, inRange, 300m, invoicedSaleId);
        // E-Arşiv: 1 adet / 60
        await SeedInvoiceAsync(EInvoiceType.EArsiv, EInvoiceStatus.Accepted, inRange, 60m);
        // Hariç olması gerekenler
        await SeedInvoiceAsync(EInvoiceType.EFatura, EInvoiceStatus.Draft, inRange, 999m);   // Draft
        await SeedInvoiceAsync(EInvoiceType.EFatura, EInvoiceStatus.Sent, outRange, 999m);   // dönem dışı

        // Faturasız: dönem içi, faturasız → SAYILIR (UnitPrice 100 * qty 2 = 200)
        await SeedSaleAsync(userId, variantId, inRange, 100m, 2);
        // Hariç: dönem dışı satış
        await SeedSaleAsync(userId, variantId, outRange, 100m, 5);
        // Hariç: iptal satış
        await SeedSaleAsync(userId, variantId, inRange, 100m, 9, SaleStatus.Cancelled);

        var sut = Sut(out var scope);
        using (scope)
        {
            var result = await sut.GetInvoiceTypeBreakdownAsync(start, end);

            Assert.Equal(2, result.EInvoice.Count);
            Assert.Equal(420m, result.EInvoice.Total);

            Assert.Equal(1, result.EArsiv.Count);
            Assert.Equal(60m, result.EArsiv.Total);

            Assert.Equal(1, result.Uninvoiced.Count);
            Assert.Equal(200m, result.Uninvoiced.Total);

            Assert.Equal(4, result.TotalCount);
            Assert.Equal(680m, result.TotalAmount);
        }
    }
}

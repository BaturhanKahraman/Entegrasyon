using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Invoicing;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// KDV Beyan Özeti — gerçek PostgreSQL ile kesilen faturaların satırlarının
/// KDV oranına göre matrah+KDV aggregate'i.
///
/// Kritik invariantlar:
///  - Sadece KESİLEN faturalar (Status=Sent/Accepted); Draft/Cancelled HARİÇ.
///  - Dönem dışı (IssueDate aralık dışı) faturalar HARİÇ.
///  - Matrah = LineTotal - TaxAmount; oran bazında SUM.
/// </summary>
[Trait("Category", "Integration")]
public class VatDeclarationReportIntegrationTests : IntegrationTestBase
{
    public VatDeclarationReportIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    private IReportManager Sut(out IServiceScope scope)
    {
        var (svc, s) = GetScopedService<IReportManager>();
        scope = s;
        return svc;
    }

    private async Task SeedInvoiceAsync(
        EInvoiceStatus status, DateTimeOffset issueDate, params (int rate, decimal taxBase, decimal vat)[] lines)
    {
        using var db = CreateDbContext();
        var invoice = new EInvoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = Guid.NewGuid().ToString("N")[..10],
            InvoiceType = EInvoiceType.EFatura,
            Status = status,
            IssueDate = issueDate,
            CreatedAt = DateTimeOffset.UtcNow,
            Lines = lines.Select(l => new EInvoiceLine
            {
                Id = Guid.NewGuid(),
                ProductName = "Ürün",
                Quantity = 1,
                UnitPrice = l.taxBase,
                TaxRate = l.rate,
                TaxAmount = l.vat,
                LineTotal = l.taxBase + l.vat,
                CreatedAt = DateTimeOffset.UtcNow
            }).ToList()
        };
        db.Set<EInvoice>().Add(invoice);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GroupsIssuedInvoiceLinesByRate_ExcludingDraftAndOutOfRange()
    {
        var start = new DateOnly(2026, 1, 1);
        var end = new DateOnly(2026, 3, 31);
        var inRange = new DateTimeOffset(2026, 2, 15, 10, 0, 0, TimeSpan.Zero);

        // Sayılması gerekenler
        await SeedInvoiceAsync(EInvoiceStatus.Sent, inRange, (20, 100m, 20m), (10, 50m, 5m));
        await SeedInvoiceAsync(EInvoiceStatus.Accepted, inRange, (20, 200m, 40m));
        // Hariç olması gerekenler
        await SeedInvoiceAsync(EInvoiceStatus.Draft, inRange, (20, 999m, 199m));               // Draft → hariç
        await SeedInvoiceAsync(EInvoiceStatus.Sent,
            new DateTimeOffset(2025, 12, 31, 10, 0, 0, TimeSpan.Zero), (20, 999m, 199m));        // dönem dışı → hariç

        var sut = Sut(out var scope);
        using (scope)
        {
            var result = await sut.GetVatDeclarationAsync(start, end);

            Assert.Equal(2, result.Lines.Count);

            var rate20 = result.Lines.Single(l => l.VatRate == 20);
            Assert.Equal(300m, rate20.TaxBase);
            Assert.Equal(60m, rate20.VatAmount);
            Assert.Equal(2, rate20.LineCount);

            var rate10 = result.Lines.Single(l => l.VatRate == 10);
            Assert.Equal(50m, rate10.TaxBase);
            Assert.Equal(5m, rate10.VatAmount);
            Assert.Equal(1, rate10.LineCount);

            Assert.Equal(350m, result.TotalTaxBase);
            Assert.Equal(65m, result.TotalVat);
        }
    }
}

using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Invoicing;
using Entegrasyon.Entity.Invoicing;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// EInvoiceManager.GetInvoiceSummary — gercek PostgreSQL'de KPI aggregate dogrulamasi.
///
/// Dogrulanacak davranislar:
/// 1. DraftCount ve SentCount sadece ilgili status'lari sayar.
/// 2. MonthGrandTotal: yalnizca BU AY + Cancelled HARIC faturalari toplar.
/// 3. Gecen ay fatuuralari MonthGrandTotal'a dahil EDILMEZ.
/// 4. Status filtresi KPI sayimlarini etkilemez (KPI'lar status'tan bagimsiz sabit sayar).
/// 5. InvoiceType filtresi hem KPI'lara hem de MonthGrandTotal'a uygulanir.
/// 6. SearchTerm filtresi KPI aggregate'e uygulanir.
/// 7. Bos tabloda hicbir hata alinmaz, tum degerler 0 gelir.
/// </summary>
[Trait("Category", "Integration")]
public class EInvoiceSummaryIntegrationTests : IntegrationTestBase
{
    public EInvoiceSummaryIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    // ── Seed helpers ────────────────────────────────────────────────────

    private async Task<EInvoice> SeedInvoiceAsync(
        EInvoiceStatus status,
        DateTimeOffset issueDate,
        decimal grandTotal = 100m,
        EInvoiceType invoiceType = EInvoiceType.EArsiv,
        string? customerTitle = null,
        string? invoiceNumber = null)
    {
        using var db = CreateDbContext();

        var invoice = new EInvoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = invoiceNumber ?? $"EFT{DateTime.UtcNow.Year}{Guid.NewGuid():N}"[..20],
            InvoiceType = invoiceType,
            Status = status,
            CustomerTaxId = "1234567890",
            CustomerTitle = customerTitle ?? "Test Musteri A.S.",
            IssueDate = issueDate,
            TotalAmount = Math.Round(grandTotal / 1.20m, 2),
            TaxAmount = Math.Round(grandTotal - grandTotal / 1.20m, 2),
            GrandTotal = grandTotal,
            IntegratorProvider = IntegratorProvider.Custom,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.EInvoices.Add(invoice);
        await db.SaveChangesAsync();
        return invoice;
    }

    private static DateTimeOffset ThisMonth(int day = 5)
    {
        var now = DateTime.UtcNow;
        return new DateTimeOffset(now.Year, now.Month, day, 10, 0, 0, TimeSpan.Zero);
    }

    private static DateTimeOffset LastMonth(int day = 5)
        => ThisMonth(day).AddMonths(-1);

    // ── Test 1: RED-first kaniti — bos tabloda 0 donmeli (hata degil) ─────

    [Fact]
    public async Task GetInvoiceSummary_EmptyTable_ReturnsAllZeros()
    {
        var (sut, scope) = GetScopedService<IEInvoiceManager>();
        using var _ = scope;

        var result = await sut.GetInvoiceSummary(new EInvoiceFilterDto());

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.DraftCount.Should().Be(0);
        result.Data.SentCount.Should().Be(0);
        result.Data.MonthGrandTotal.Should().Be(0m);
    }

    // ── Test 2: DraftCount ve SentCount dogruluğu ─────────────────────────

    [Fact]
    public async Task GetInvoiceSummary_CountsDraftAndSentCorrectly()
    {
        // Arrange: 2 Draft + 3 Sent + 1 Accepted + 1 Rejected + 1 Cancelled
        await SeedInvoiceAsync(EInvoiceStatus.Draft, ThisMonth());
        await SeedInvoiceAsync(EInvoiceStatus.Draft, ThisMonth());
        await SeedInvoiceAsync(EInvoiceStatus.Sent, ThisMonth());
        await SeedInvoiceAsync(EInvoiceStatus.Sent, ThisMonth());
        await SeedInvoiceAsync(EInvoiceStatus.Sent, ThisMonth());
        await SeedInvoiceAsync(EInvoiceStatus.Accepted, ThisMonth());
        await SeedInvoiceAsync(EInvoiceStatus.Rejected, ThisMonth());
        await SeedInvoiceAsync(EInvoiceStatus.Cancelled, ThisMonth());

        var (sut, scope) = GetScopedService<IEInvoiceManager>();
        using var _ = scope;

        var result = await sut.GetInvoiceSummary(new EInvoiceFilterDto());

        result.Success.Should().BeTrue();
        result.Data!.DraftCount.Should().Be(2);
        result.Data.SentCount.Should().Be(3);
    }

    // ── Test 3: MonthGrandTotal — bu-ay + Cancelled HARIC ─────────────────

    [Fact]
    public async Task GetInvoiceSummary_MonthGrandTotal_ExcludesCancelledAndLastMonth()
    {
        // Arrange: bu-ay (Draft=200, Sent=300, Accepted=400, Cancelled=999[HARIC])
        //          gecen-ay (Draft=500 [HARIC]) + bu-ay Rejected=150
        await SeedInvoiceAsync(EInvoiceStatus.Draft, ThisMonth(), grandTotal: 200m);
        await SeedInvoiceAsync(EInvoiceStatus.Sent, ThisMonth(), grandTotal: 300m);
        await SeedInvoiceAsync(EInvoiceStatus.Accepted, ThisMonth(), grandTotal: 400m);
        await SeedInvoiceAsync(EInvoiceStatus.Rejected, ThisMonth(), grandTotal: 150m);
        await SeedInvoiceAsync(EInvoiceStatus.Cancelled, ThisMonth(), grandTotal: 999m); // HARIC
        await SeedInvoiceAsync(EInvoiceStatus.Draft, LastMonth(), grandTotal: 500m);     // HARIC (gecen ay)

        var (sut, scope) = GetScopedService<IEInvoiceManager>();
        using var _ = scope;

        var result = await sut.GetInvoiceSummary(new EInvoiceFilterDto());

        result.Success.Should().BeTrue();
        // Beklenen: 200 + 300 + 400 + 150 = 1050 (Cancelled 999 ve gecen ay 500 haric)
        result.Data!.MonthGrandTotal.Should().Be(1050m);
    }

    // ── Test 4: Status filtresi KPI'lari etkilemez ────────────────────────

    [Fact]
    public async Task GetInvoiceSummary_StatusFilter_DoesNotAffectKpiCounts()
    {
        // Arrange: 2 Draft + 3 Sent
        await SeedInvoiceAsync(EInvoiceStatus.Draft, ThisMonth());
        await SeedInvoiceAsync(EInvoiceStatus.Draft, ThisMonth());
        await SeedInvoiceAsync(EInvoiceStatus.Sent, ThisMonth());
        await SeedInvoiceAsync(EInvoiceStatus.Sent, ThisMonth());
        await SeedInvoiceAsync(EInvoiceStatus.Sent, ThisMonth());

        var (sut, scope) = GetScopedService<IEInvoiceManager>();
        using var _ = scope;

        // Kullanici tabloda sadece Draft'lari gormek icin Status filtresi uyguluyor
        // ama KPI kartlari yine de tum fatuuralarin sayimini gostermeli
        var filterWithStatus = new EInvoiceFilterDto { Status = EInvoiceStatus.Draft };
        var result = await sut.GetInvoiceSummary(filterWithStatus);

        result.Success.Should().BeTrue();
        // KPI'lar status'tan bagimsiz: Draft=2, Sent=3 hepsi gorunmeli
        result.Data!.DraftCount.Should().Be(2, "Status filtresi KPI sayimlarini daraltmamali");
        result.Data.SentCount.Should().Be(3, "Status filtresi KPI sayimlarini daraltmamali");
    }

    // ── Test 5: InvoiceType filtresi KPI'lara uygulanir ──────────────────

    [Fact]
    public async Task GetInvoiceSummary_InvoiceTypeFilter_AppliedToKpiCounts()
    {
        // Arrange: 2 Draft EArsiv + 1 Draft EFatura + 1 Sent EArsiv
        await SeedInvoiceAsync(EInvoiceStatus.Draft, ThisMonth(), invoiceType: EInvoiceType.EArsiv);
        await SeedInvoiceAsync(EInvoiceStatus.Draft, ThisMonth(), invoiceType: EInvoiceType.EArsiv);
        await SeedInvoiceAsync(EInvoiceStatus.Draft, ThisMonth(), invoiceType: EInvoiceType.EFatura);
        await SeedInvoiceAsync(EInvoiceStatus.Sent, ThisMonth(), invoiceType: EInvoiceType.EArsiv);

        var (sut, scope) = GetScopedService<IEInvoiceManager>();
        using var _ = scope;

        var filterEArsiv = new EInvoiceFilterDto { InvoiceType = EInvoiceType.EArsiv };
        var result = await sut.GetInvoiceSummary(filterEArsiv);

        result.Success.Should().BeTrue();
        // Yalnizca EArsiv: Draft=2, Sent=1
        result.Data!.DraftCount.Should().Be(2);
        result.Data.SentCount.Should().Be(1);
    }

    // ── Test 6: InvoiceType filtresi MonthGrandTotal'a da uygulanir ───────

    [Fact]
    public async Task GetInvoiceSummary_InvoiceTypeFilter_AppliedToMonthGrandTotal()
    {
        // Arrange: bu-ay EArsiv=500, bu-ay EFatura=300
        await SeedInvoiceAsync(EInvoiceStatus.Sent, ThisMonth(), grandTotal: 500m, invoiceType: EInvoiceType.EArsiv);
        await SeedInvoiceAsync(EInvoiceStatus.Sent, ThisMonth(), grandTotal: 300m, invoiceType: EInvoiceType.EFatura);

        var (sut, scope) = GetScopedService<IEInvoiceManager>();
        using var _ = scope;

        var resultEArsiv = await sut.GetInvoiceSummary(new EInvoiceFilterDto { InvoiceType = EInvoiceType.EArsiv });
        var resultAll = await sut.GetInvoiceSummary(new EInvoiceFilterDto());

        resultEArsiv.Data!.MonthGrandTotal.Should().Be(500m, "EArsiv filtresi sadece EArsiv toplamini vermeli");
        resultAll.Data!.MonthGrandTotal.Should().Be(800m, "Filtresiz tum fatura toplami gelmeli");
    }

    // ── Test 7: SearchTerm filtresi KPI'a uygulanir ───────────────────────

    [Fact]
    public async Task GetInvoiceSummary_SearchTermFilter_AppliedToKpiCounts()
    {
        // Arrange: iki farkli musteri
        await SeedInvoiceAsync(EInvoiceStatus.Draft, ThisMonth(), customerTitle: "ABC Ticaret Ltd.");
        await SeedInvoiceAsync(EInvoiceStatus.Draft, ThisMonth(), customerTitle: "ABC Ticaret Ltd.");
        await SeedInvoiceAsync(EInvoiceStatus.Draft, ThisMonth(), customerTitle: "XYZ Holding A.S.");
        await SeedInvoiceAsync(EInvoiceStatus.Sent, ThisMonth(), customerTitle: "XYZ Holding A.S.");

        var (sut, scope) = GetScopedService<IEInvoiceManager>();
        using var _ = scope;

        // "ABC" aramasinda KPI sadece ABC faturalarini sayar
        var filterAbc = new EInvoiceFilterDto { SearchTerm = "ABC" };
        var result = await sut.GetInvoiceSummary(filterAbc);

        result.Success.Should().BeTrue();
        result.Data!.DraftCount.Should().Be(2, "SearchTerm filtresi KPI'a uygulanmali");
        result.Data.SentCount.Should().Be(0, "ABC musteri icin Sent fatura yok");
    }

    // ── Test 8: Kritik — gecen ay faturalari bu-ay toplamina GIRMEZ ───────

    [Fact]
    public async Task GetInvoiceSummary_LastMonthInvoices_NotIncludedInMonthGrandTotal()
    {
        // Arrange: sadece gecen ay fatuurasi
        await SeedInvoiceAsync(EInvoiceStatus.Sent, LastMonth(), grandTotal: 9999m);

        var (sut, scope) = GetScopedService<IEInvoiceManager>();
        using var _ = scope;

        var result = await sut.GetInvoiceSummary(new EInvoiceFilterDto());

        result.Success.Should().BeTrue();
        // Gecen ay faturasi bu-ay toplamina girmemeli
        result.Data!.MonthGrandTotal.Should().Be(0m, "Gecen ay fatuuralari bu-ay toplamina dahil edilmemeli");
        // Ama DraftCount/SentCount'a girer (tarih bagimsiz sayim)
        result.Data.SentCount.Should().Be(1, "Tarih filtresi olmadan SentCount tum zamanlari sayar");
    }

    // ── Test 9: Tam senaryo — gorev briefindeki seed ──────────────────────

    /// <summary>
    /// Gorev tanimi: 2 Draft + 3 Sent + 1 Cancelled + farkli IssueDate (bu-ay/gecen-ay).
    /// Bu-ay: 1 Draft(100) + 2 Sent(200+300) + 1 Cancelled(400[HARIC]) + 1 Sent(gecen-ay=500[HARIC])
    /// DraftCount=2, SentCount=3, MonthGrandTotal=100+200+300=600.
    /// </summary>
    [Fact]
    public async Task GetInvoiceSummary_FullScenario_MatchesBriefSpecification()
    {
        // Arrange — brief'teki seed tanimi
        // Bu ay:
        await SeedInvoiceAsync(EInvoiceStatus.Draft, ThisMonth(3), grandTotal: 100m);   // bu ay draft
        await SeedInvoiceAsync(EInvoiceStatus.Draft, ThisMonth(7), grandTotal: 150m);   // bu ay draft (gecen aya gecmeyecek)
        await SeedInvoiceAsync(EInvoiceStatus.Sent, ThisMonth(10), grandTotal: 200m);   // bu ay sent
        await SeedInvoiceAsync(EInvoiceStatus.Sent, ThisMonth(12), grandTotal: 300m);   // bu ay sent
        await SeedInvoiceAsync(EInvoiceStatus.Cancelled, ThisMonth(15), grandTotal: 400m); // bu ay cancelled — MonthGrandTotal'a GIRMEZ
        // Gecen ay:
        await SeedInvoiceAsync(EInvoiceStatus.Sent, LastMonth(20), grandTotal: 500m);   // gecen ay sent — MonthGrandTotal'a GIRMEZ

        var (sut, scope) = GetScopedService<IEInvoiceManager>();
        using var _ = scope;

        var result = await sut.GetInvoiceSummary(new EInvoiceFilterDto());

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();

        // DraftCount: 2 (tarih/gecen-ay bagimsiz, sadece status=Draft)
        result.Data!.DraftCount.Should().Be(2, "2 Draft fatura olmali (bu ay + gecen ay dahil)");

        // SentCount: 3 (tarih bagimsiz — bu-ay 2 + gecen-ay 1)
        result.Data.SentCount.Should().Be(3, "3 Sent fatura olmali (bu ay 2 + gecen ay 1)");

        // MonthGrandTotal: sadece bu-ay + Cancelled haric
        // = 100 (Draft-bu-ay) + 150 (Draft-bu-ay) + 200 (Sent-bu-ay) + 300 (Sent-bu-ay)
        // Cancelled(400) ve gecen-ay(500) haric
        result.Data.MonthGrandTotal.Should().Be(750m,
            "MonthGrandTotal: bu-ay Draft(100+150) + Sent(200+300) = 750; Cancelled ve gecen-ay haric");
    }
}

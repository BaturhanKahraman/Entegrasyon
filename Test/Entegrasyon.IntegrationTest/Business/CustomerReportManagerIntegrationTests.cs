using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity.DiscountVouchers;
using Entegrasyon.Entity.Dtos.Reports;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Orders;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// CustomerReportManager — RFM segmentasyon + cohort retention + dormant geri kazanım kuponu.
///
/// RFM eşikleri (brief):
///   VIP     = son 30 gün içinde alışveriş + toplam harcama tüm müşteriler arasında üst %20 persentil
///   Risk    = son alışverişinden 90+ gün geçmiş (ama daha önce alışveriş yapmış)
///   Dormant = 90+ gün alışveriş yok (Risk ile aynı recency koşulu; brief'te ayrı KPI/filtre)
///   Yeni    = son 30 gün içinde kayıt (CreatedAt) + hiç/yeni alışveriş
///
/// Recency/monetary Order(CustomerId, OrderDate, GrossAmount) üzerinden; Postgres GroupBy +
/// percentile semantiği ancak gerçek DB'de doğrulanır.
/// </summary>
[Trait("Category", "Integration")]
public class CustomerReportManagerIntegrationTests : IntegrationTestBase
{
    public CustomerReportManagerIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    private static Address Addr() => new() { City = "Istanbul", Country = "TR", FullAddress = "x" };

    private async Task<int> SeedCustomerAsync(string fullName, DateTimeOffset createdAt)
    {
        using var db = CreateDbContext();
        var c = new RetailCustomer
        {
            FullName = fullName,
            CustomerType = "Retail",
            IsActive = true,
            CreatedAt = createdAt,
            Address = Addr()
        };
        db.Customers.Add(c);
        await db.SaveChangesAsync();
        return c.Id;
    }

    private async Task SeedOrderAsync(int customerId, DateTimeOffset orderDate, decimal grossAmount)
    {
        using var db = CreateDbContext();
        db.Orders.Add(new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            OrderDate = orderDate,
            GrossAmount = grossAmount,
            CreatedAt = orderDate,
            BillingAddress = Addr(),
            ShippingAddress = Addr()
        });
        await db.SaveChangesAsync();
    }

    private ICustomerReportManager Sut()
    {
        var (svc, _) = GetScopedService<ICustomerReportManager>();
        return svc;
    }

    // ── RFM segmentasyon ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRfmPageable_AssignsVip_ForRecentTopSpender()
    {
        var now = DateTimeOffset.UtcNow;

        // Yüksek harcamalı + son 30 gün alışveriş → VIP (üst %20).
        var vipId = await SeedCustomerAsync("Vip Musteri", now.AddMonths(-6));
        await SeedOrderAsync(vipId, now.AddDays(-5), 10_000m);

        // Düşük harcamalı, son alışveriş yakın ama üst persentilde değil → Standart.
        for (var i = 0; i < 5; i++)
        {
            var lowId = await SeedCustomerAsync($"Dusuk {i}", now.AddMonths(-6));
            await SeedOrderAsync(lowId, now.AddDays(-3), 100m);
        }

        var result = await Sut().GetRfmPageableAsync(segment: null, pageIndex: 0, itemCount: 50);

        result.Success.Should().BeTrue(result.Message);
        var vip = result.Data!.Items.Single(x => x.Id == vipId);
        vip.RfmSegment.Should().Be("VIP");
        vip.TotalSpend.Should().Be(10_000m);
        vip.LastPurchaseDate.Should().Be(DateOnly.FromDateTime(now.AddDays(-5).UtcDateTime));
    }

    [Fact]
    public async Task GetRfmPageable_AssignsDormant_For90PlusDaysNoPurchase()
    {
        var now = DateTimeOffset.UtcNow;
        var dormantId = await SeedCustomerAsync("Uyuyan", now.AddMonths(-12));
        await SeedOrderAsync(dormantId, now.AddDays(-120), 500m);

        var result = await Sut().GetRfmPageableAsync(segment: null);

        result.Success.Should().BeTrue(result.Message);
        var d = result.Data!.Items.Single(x => x.Id == dormantId);
        d.RfmSegment.Should().Be("Dormant");
    }

    [Fact]
    public async Task GetRfmPageable_AssignsYeni_ForRecentlyRegistered()
    {
        var now = DateTimeOffset.UtcNow;
        var newId = await SeedCustomerAsync("Taze", now.AddDays(-3));   // hiç sipariş yok

        var result = await Sut().GetRfmPageableAsync(segment: null);

        var c = result.Data!.Items.Single(x => x.Id == newId);
        c.RfmSegment.Should().Be("Yeni");
    }

    [Fact]
    public async Task GetRfmPageable_DormantSegmentFilter_ReturnsOnlyDormant()
    {
        var now = DateTimeOffset.UtcNow;
        var dormantId = await SeedCustomerAsync("Uyuyan", now.AddMonths(-12));
        await SeedOrderAsync(dormantId, now.AddDays(-120), 500m);
        var activeId = await SeedCustomerAsync("Aktif", now.AddMonths(-6));
        await SeedOrderAsync(activeId, now.AddDays(-2), 500m);

        var result = await Sut().GetRfmPageableAsync(segment: "dormant");

        result.Data!.Items.Should().OnlyContain(x => x.Id == dormantId);
    }

    // ── Cohort retention ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCohortRetention_ComputesMonthOverMonthRetention()
    {
        // Aynı kohort (ilk-alışveriş ayı) — 2 müşteri; biri sonraki ay tekrar alır, biri almaz.
        var anchor = new DateTimeOffset(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 15, 0, 0, 0, TimeSpan.Zero)
            .AddMonths(-2);

        var retainedId = await SeedCustomerAsync("Sadik", anchor.AddMonths(-1));
        await SeedOrderAsync(retainedId, anchor, 100m);           // 0. ay
        await SeedOrderAsync(retainedId, anchor.AddMonths(1), 100m); // +1 ay → retained

        var churnedId = await SeedCustomerAsync("Kacan", anchor.AddMonths(-1));
        await SeedOrderAsync(churnedId, anchor, 100m);           // 0. ay, sonra yok

        var cohort = await Sut().GetCohortRetentionAsync();

        cohort.Should().NotBeEmpty();
        var row = cohort.First(r => r.CohortMonth == anchor.ToString("yyyy-MM"));
        row.Size.Should().Be(2);
        row.RetentionPct[0].Should().Be(100);     // tanım gereği
        row.RetentionPct[1].Should().Be(50);      // 2'den 1'i tekrar aldı
    }

    // ── Sendable coupons ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSendableCoupons_ReturnsActiveNonExpired()
    {
        using (var db = CreateDbContext())
        {
            db.DiscountVouchers.AddRange(
                new DiscountVoucher { Code = "AKTIF1", IsActive = true, DiscountType = DiscountType.Percentage, Percentage = 10, CreatedAt = DateTimeOffset.UtcNow },
                new DiscountVoucher { Code = "PASIF1", IsActive = false, DiscountType = DiscountType.FixedAmount, Amount = 50, CreatedAt = DateTimeOffset.UtcNow },
                new DiscountVoucher { Code = "GECMIS", IsActive = true, DiscountType = DiscountType.FixedAmount, Amount = 50, ExpiringDate = DateTimeOffset.UtcNow.AddDays(-1), CreatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
        }

        var coupons = await Sut().GetSendableCouponsAsync();

        coupons.Select(c => c.Code).Should().Contain("AKTIF1");
        coupons.Select(c => c.Code).Should().NotContain("PASIF1");
        coupons.Select(c => c.Code).Should().NotContain("GECMIS");
    }

    // ── MUTATION: send recovery coupon (no-tracking persist footgun) ───────────────

    [Fact]
    public async Task SendRecoveryCoupon_PersistsPerCustomerVoucher_AndWritesLog()
    {
        var now = DateTimeOffset.UtcNow;
        var c1 = await SeedCustomerAsync("Hedef 1", now.AddMonths(-6));
        var c2 = await SeedCustomerAsync("Hedef 2", now.AddMonths(-6));

        int voucherId;
        using (var db = CreateDbContext())
        {
            var v = new DiscountVoucher
            {
                Code = "GERIKAZAN",
                IsActive = true,
                DiscountType = DiscountType.Percentage,
                Percentage = 15,
                CreatedAt = now
            };
            db.DiscountVouchers.Add(v);
            await db.SaveChangesAsync();
            voucherId = v.Id;
        }

        var dto = new SendRecoveryCouponDto(voucherId, new[] { c1, c2 }, "dormant");
        var result = await Sut().SendRecoveryCouponAsync(dto, Guid.NewGuid());

        result.Success.Should().BeTrue(result.Message);

        using var verify = CreateDbContext();
        // Her müşteriye birer kupon bağlandı (orijinal + 2 kopya = 3, ya da orijinal müşteriye atandı).
        var perCustomer = await verify.DiscountVouchers
            .AsNoTracking()
            .Where(v => v.CustomerId == c1 || v.CustomerId == c2)
            .ToListAsync();
        perCustomer.Select(v => v.CustomerId).Should().BeEquivalentTo(new int?[] { c1, c2 });
        perCustomer.Should().OnlyContain(v => v.IsActive);

        var log = await verify.Logs.AsNoTracking()
            .Where(l => l.LogType == LogType.DiscountVoucher)
            .OrderByDescending(l => l.Id)
            .FirstOrDefaultAsync();
        log.Should().NotBeNull();
    }

    [Fact]
    public async Task SendRecoveryCoupon_EmptyCustomerIds_FailsValidation()
    {
        int voucherId;
        using (var db = CreateDbContext())
        {
            var v = new DiscountVoucher { Code = "X", IsActive = true, DiscountType = DiscountType.Percentage, Percentage = 10, CreatedAt = DateTimeOffset.UtcNow };
            db.DiscountVouchers.Add(v);
            await db.SaveChangesAsync();
            voucherId = v.Id;
        }

        var dto = new SendRecoveryCouponDto(voucherId, Array.Empty<int>(), "dormant");

        var act = async () => await Sut().SendRecoveryCouponAsync(dto, Guid.NewGuid());

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
    }
}

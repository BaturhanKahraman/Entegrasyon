using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Sales;
using Entegrasyon.Entity.Settings;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// Ayar sayfalarindaki "kalici hale gelmiyor" bug'larini gercek (no-tracking)
/// EF Core context ile yeniden uretir. Moq tabanli unit testler no-tracking
/// davranisini taklit edemedigi icin (mock ayni in-memory nesneyi dondurur,
/// SaveChanges no-op olsa bile mutasyon gorunur) bu testler integration
/// seviyesinde yazilmistir.
/// </summary>
[Trait("Category", "Integration")]
public class SettingsPersistenceIntegrationTests : IntegrationTestBase
{
    public SettingsPersistenceIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    // ─── Bug 1: KDV varsayilan ayari kalici degil ───────────────────────────

    [Fact]
    public async Task SetDefaultVatRate_ShouldPersist_AndKeepSingleDefault()
    {
        // Arrange: iki KDV orani, ilki varsayilan
        int targetId;
        using (var seed = CreateDbContext())
        {
            var rate1 = new VatRate { Name = "Standart", Rate = 20, IsDefault = true, IsActive = true };
            var rate2 = new VatRate { Name = "Indirimli", Rate = 10, IsDefault = false, IsActive = true };
            seed.VatRates.AddRange(rate1, rate2);
            await seed.SaveChangesAsync();
            targetId = rate2.Id;
        }

        var (manager, scope) = GetScopedService<IVatRateManager>();
        using var _ = scope;

        // Act: ikinci orani varsayilan yap
        var result = await manager.SetDefaultAsync(targetId);

        // Assert: islem basarili + DB'de gercekten kalici
        result.Success.Should().BeTrue(result.Message);

        using var db = CreateDbContext();
        var defaults = await db.VatRates.AsNoTracking().Where(v => v.IsDefault).ToListAsync();

        // Tek-varsayilan invariant'i: tam olarak 1 tane varsayilan olmali
        defaults.Should().HaveCount(1);
        // ve o, az once secilen oran olmali (persist edilmis olmali)
        defaults[0].Id.Should().Be(targetId);
    }

    // ─── Bug 2: Odeme yontemi siralamasi kalici degil ───────────────────────

    [Fact]
    public async Task ReorderPaymentMethods_ShouldPersistSortOrders()
    {
        // Arrange: ucuncu bir tenant icin uc odeme yontemi (PaymentMethodDefinitions
        // Respawn tarafindan korunan seed tablo; izolasyon icin ayri tenant kullan)
        const int tenantId = 9001;
        int id1, id2, id3;
        using (var seed = CreateDbContext())
        {
            var m1 = new PaymentMethodDefinition { Name = "Nakit", SystemCode = "T9001_CASH", IsActive = true, SortOrder = 1, TenantId = tenantId };
            var m2 = new PaymentMethodDefinition { Name = "Kredi", SystemCode = "T9001_CREDIT", IsActive = true, SortOrder = 2, TenantId = tenantId };
            var m3 = new PaymentMethodDefinition { Name = "Havale", SystemCode = "T9001_BANK", IsActive = true, SortOrder = 3, TenantId = tenantId };
            seed.PaymentMethodDefinitions.AddRange(m1, m2, m3);
            await seed.SaveChangesAsync();
            id1 = m1.Id; id2 = m2.Id; id3 = m3.Id;
        }

        var (manager, scope) = GetScopedService<IPaymentMethodManager>();
        using var _ = scope;

        // Act: sirayi tersine cevir → 3, 1, 2
        var result = await manager.ReorderPaymentMethodsAsync([id3, id1, id2]);

        // Assert: yeni siralama DB'de kalici olmali
        result.Success.Should().BeTrue(result.Message);

        using var db = CreateDbContext();
        var byId = await db.PaymentMethodDefinitions
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .ToDictionaryAsync(x => x.Id, x => x.SortOrder);

        byId[id3].Should().Be(1);
        byId[id1].Should().Be(2);
        byId[id2].Should().Be(3);
    }
}

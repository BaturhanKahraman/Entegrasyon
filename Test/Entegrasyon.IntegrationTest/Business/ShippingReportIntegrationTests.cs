using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Reports;
using Entegrasyon.Entity.Orders;
using Entegrasyon.Entity.Shipping;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// Kargo Raporu analitiği (ShipmentTracking) — gerçek PostgreSQL.
///
/// Kapsam: firma performansı, bölge yoğunluğu, gecikme trendi, gecikmiş gönderi listesi
/// + toplu müşteri bilgilendirme (MUTASYON).
/// Gecikme = teslim-geç (Actual > Estimated) VEYA yolda-gecikmiş (terminal değil + Estimated < şimdi).
/// </summary>
[Trait("Category", "Integration")]
public class ShippingReportIntegrationTests : IntegrationTestBase
{
    public ShippingReportIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    private static readonly DateTimeOffset MayCreate = new(2026, 5, 2, 8, 0, 0, TimeSpan.Zero);

    private IReportManager Report(out IServiceScope scope)
    {
        var (svc, s) = GetScopedService<IReportManager>();
        scope = s;
        return svc;
    }

    private async Task<int> SeedCargoCompanyAsync(string name)
    {
        using var db = CreateDbContext();
        var c = new CargoCompany { Name = name, CreatedAt = DateTimeOffset.UtcNow };
        db.Set<CargoCompany>().Add(c);
        await db.SaveChangesAsync();
        return c.Id;
    }

    private async Task<Guid> SeedOrderAsync(string city, string? customerEmail)
    {
        using var db = CreateDbContext();
        Address Addr() => new() { City = city, Country = "Türkiye", FullAddress = "Test Adres" };
        var id = Guid.NewGuid();
        db.Set<Order>().Add(new Order
        {
            Id = id,
            OrderDate = MayCreate,
            OrderNumber = $"O-{Guid.NewGuid():N}"[..12],
            CustomerEmail = customerEmail,
            CustomerFirstName = "Test",
            BillingAddress = Addr(),
            ShippingAddress = Addr(),
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
        return id;
    }

    private async Task<long> SeedShipmentAsync(
        int cargoId, ShipmentStatus status, DateTimeOffset? estimated, DateTimeOffset? actual, Guid? orderId = null)
    {
        using var db = CreateDbContext();
        var s = new ShipmentTracking
        {
            CargoCompanyId = cargoId,
            OrderId = orderId,
            TrackingNumber = $"TN-{Guid.NewGuid():N}"[..12],
            CurrentStatus = status,
            EstimatedDeliveryDate = estimated,
            ActualDeliveryDate = actual,
            RecipientName = "Alıcı",
            CreatedAt = MayCreate
        };
        db.Set<ShipmentTracking>().Add(s);
        await db.SaveChangesAsync();
        return s.Id;
    }

    [Fact]
    public async Task ShippingAnalytics_Performance_Region_Trend_DelayedList()
    {
        // ShipmentTracking.CreatedAt insert'te UtcNow'a set edilir (audit interceptor) →
        // perf/region/list CreatedAt'e göre filtrelediği için dönem "şimdi"yi kapsamalı.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var start = today.AddDays(-15);
        var end = today;
        var estPast = DateTimeOffset.UtcNow.AddDays(-5);   // dönem içi, geçmiş
        var estFuture = DateTimeOffset.UtcNow.AddDays(30); // dönem dışı, gelecek

        await SeedBasicEntitiesAsync();
        var cargoId = await SeedCargoCompanyAsync("Aras Kargo");
        var orderAnkara = await SeedOrderAsync("Ankara", "musteri@example.com");

        // 1) Teslim — zamanında (actual <= estimated): gecikme DEĞİL
        await SeedShipmentAsync(cargoId, ShipmentStatus.Delivered, estPast, estPast.AddDays(-1));
        // 2) Teslim — geç (actual > estimated): GECİKME, Ankara order + email
        await SeedShipmentAsync(cargoId, ShipmentStatus.Delivered, estPast, estPast.AddDays(2), orderAnkara);
        // 3) Yolda — gecikmiş (estimated geçmiş, terminal değil): GECİKME
        await SeedShipmentAsync(cargoId, ShipmentStatus.InTransit, estPast, null, orderAnkara);
        // 4) Yolda — gecikmemiş (estimated gelecek): gecikme DEĞİL (trend penceresi dışı)
        await SeedShipmentAsync(cargoId, ShipmentStatus.InTransit, estFuture, null);

        var sut = Report(out var scope);
        using (scope)
        {
            // --- Firma performansı ---
            var perf = await sut.GetCargoCompanyPerformanceAsync(start, end);
            var aras = perf.Single(p => p.CargoCompanyName == "Aras Kargo");
            Assert.Equal(4, aras.TotalShipments);
            Assert.Equal(2, aras.DeliveredCount);
            Assert.Equal(2, aras.DelayedCount);
            Assert.Equal(50.0, aras.DeliveryRatePercent);
            Assert.Equal(50.0, aras.DelayRatePercent);

            // --- Bölge yoğunluğu ---
            var region = await sut.GetRegionDensityAsync(start, end);
            Assert.Equal(2, region.Single(r => r.City == "Ankara").ShipmentCount);
            Assert.Equal(2, region.Single(r => r.City == "Bilinmeyen").ShipmentCount);

            // --- Gecikme trendi (estimated dönem içi: estPast×3, estFuture hariç) ---
            var trend = await sut.GetDelayTrendAsync(start, end);
            Assert.Equal(2, trend.Sum(t => t.DelayedCount));
            Assert.Equal(3, trend.Sum(t => t.TotalCount));

            // --- Gecikmiş gönderi listesi ---
            var delayed = await sut.GetDelayedShipmentsAsync(start, end);
            Assert.Equal(2, delayed.Count);
            Assert.Contains(delayed, d => d.HasCustomerEmail);   // Ankara order'ı email taşıyor
            Assert.All(delayed, d => Assert.True(d.DaysLate >= 0));
        }
    }

    [Fact]
    public async Task NotifyDelayedCustomers_RulesAndExecution()
    {
        var start = new DateOnly(2026, 5, 1);
        var end = new DateOnly(2026, 5, 31);
        var estPast = new DateTimeOffset(2026, 5, 5, 10, 0, 0, TimeSpan.Zero);

        await SeedBasicEntitiesAsync();
        var cargoId = await SeedCargoCompanyAsync("Yurtiçi");
        var orderEmail = await SeedOrderAsync("İzmir", "musteri@example.com");
        var shipmentId = await SeedShipmentAsync(cargoId, ShipmentStatus.InTransit, estPast, null, orderEmail);

        var (mgr, scope) = GetScopedService<IShipmentDelayNotificationManager>();
        using (scope)
        {
            // Geçerli gönderi → Success (SMTP yok → e-posta gönderilemez ama toplu işlem başarılı sayılır)
            var ok = await mgr.NotifyDelayedCustomersAsync(
                new NotifyDelayedShipmentsDto(new List<long> { shipmentId }), userId: null);
            Assert.True(ok.Success);

            // Var olmayan gönderi → iş kuralı hatası
            var notFound = await mgr.NotifyDelayedCustomersAsync(
                new NotifyDelayedShipmentsDto(new List<long> { 999999 }), userId: null);
            Assert.False(notFound.Success);
            Assert.Contains("bulunamadı", notFound.Message);
        }
    }
}

using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Shipping;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// ShipmentTrackingManager.GetShipmentKpisAsync — Kargo liste sayfası KPI snapshot'ı.
///
/// Tek server-side GroupBy(CurrentStatus) → count; kovalar (Yolda/Teslim/Sorunlu) bellekte
/// pivot. Status bucket birleştirme (InTransit+OutForDelivery, Failed+ReturnedToSender) ve
/// soft-delete query filter semantiği ancak gerçek Postgres'te doğrulanabilir.
/// </summary>
[Trait("Category", "Integration")]
public class ShipmentKpiIntegrationTests : IntegrationTestBase
{
    public ShipmentKpiIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    private async Task<int> SeedCargoCompanyAsync()
    {
        using var db = CreateDbContext();
        var company = new CargoCompany { Name = "Test Kargo" };
        db.Set<CargoCompany>().Add(company);
        await db.SaveChangesAsync();
        return company.Id;
    }

    private async Task SeedShipmentAsync(int cargoCompanyId, ShipmentStatus status, bool isDeleted = false)
    {
        using var db = CreateDbContext();
        db.ShipmentTrackings.Add(new ShipmentTracking
        {
            CargoCompanyId = cargoCompanyId,
            TrackingNumber = $"TRK-{Guid.NewGuid():N}"[..16],
            CurrentStatus = status,
            IsDeleted = isDeleted,
            DeletedAt = isDeleted ? DateTimeOffset.UtcNow : default,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
    }

    private IShipmentTrackingManager Sut(out IServiceScope scope)
    {
        var (svc, s) = GetScopedService<IShipmentTrackingManager>();
        scope = s;
        return svc;
    }

    [Fact]
    public async Task EmptyDb_ReturnsAllZeros()
    {
        var sut = Sut(out var scope);
        using var _ = scope;

        var result = await sut.GetShipmentKpisAsync();

        result.InTransitCount.Should().Be(0);
        result.DeliveredCount.Should().Be(0);
        result.ProblemCount.Should().Be(0);
    }

    [Fact]
    public async Task BucketsCombineStatusesCorrectly()
    {
        var company = await SeedCargoCompanyAsync();

        // Yolda = InTransit(2) + OutForDelivery(1) = 3
        await SeedShipmentAsync(company, ShipmentStatus.InTransit);
        await SeedShipmentAsync(company, ShipmentStatus.InTransit);
        await SeedShipmentAsync(company, ShipmentStatus.OutForDelivery);
        // Teslim = Delivered(2)
        await SeedShipmentAsync(company, ShipmentStatus.Delivered);
        await SeedShipmentAsync(company, ShipmentStatus.Delivered);
        // Sorunlu = Failed(1) + ReturnedToSender(2) = 3
        await SeedShipmentAsync(company, ShipmentStatus.Failed);
        await SeedShipmentAsync(company, ShipmentStatus.ReturnedToSender);
        await SeedShipmentAsync(company, ShipmentStatus.ReturnedToSender);
        // Hiçbir kovaya girmeyen (Created/PickedUp/Cancelled) — sayılmamalı
        await SeedShipmentAsync(company, ShipmentStatus.Created);
        await SeedShipmentAsync(company, ShipmentStatus.PickedUp);
        await SeedShipmentAsync(company, ShipmentStatus.Cancelled);

        var sut = Sut(out var scope);
        using var _ = scope;

        var result = await sut.GetShipmentKpisAsync();

        result.InTransitCount.Should().Be(3);   // InTransit + OutForDelivery (PickedUp HARİÇ)
        result.DeliveredCount.Should().Be(2);
        result.ProblemCount.Should().Be(3);     // Failed + ReturnedToSender
    }

    [Fact]
    public async Task SoftDeletedShipments_ExcludedFromAllBuckets()
    {
        var company = await SeedCargoCompanyAsync();

        await SeedShipmentAsync(company, ShipmentStatus.InTransit);
        await SeedShipmentAsync(company, ShipmentStatus.Delivered, isDeleted: true);  // hariç
        await SeedShipmentAsync(company, ShipmentStatus.Failed, isDeleted: true);     // hariç

        var sut = Sut(out var scope);
        using var _ = scope;

        var result = await sut.GetShipmentKpisAsync();

        result.InTransitCount.Should().Be(1);
        result.DeliveredCount.Should().Be(0);   // silinmiş
        result.ProblemCount.Should().Be(0);     // silinmiş
    }
}

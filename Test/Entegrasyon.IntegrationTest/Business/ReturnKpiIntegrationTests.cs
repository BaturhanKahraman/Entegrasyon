using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Orders;
using Entegrasyon.Entity.Sales;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// SaleReturnManager.GetReturnKpisAsync — İade liste sayfası KPI snapshot'ı.
///
/// Tek GroupBy(ReturnStatus) → count + Completed-only conditional RefundAmount sum.
/// Status bazlı sayım, koşullu toplam ve soft-delete query filter semantiği ancak gerçek
/// Postgres'te doğrulanabilir.
/// </summary>
[Trait("Category", "Integration")]
public class ReturnKpiIntegrationTests : IntegrationTestBase
{
    public ReturnKpiIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    private static Address Addr() => new() { City = "Istanbul", Country = "TR", FullAddress = "x" };

    private async Task SeedReturnAsync(ReturnStatus status, decimal refundAmount, bool isDeleted = false)
    {
        using var db = CreateDbContext();
        // CK_SaleReturn_SaleOrOrder: SaleId XOR OrderId — bir Order'a bağlayarak OrderId set ederiz.
        var orderId = Guid.NewGuid();
        db.Orders.Add(new Order
        {
            Id = orderId,
            OrderNumber = $"ORD-{orderId:N}"[..16],
            OrderDate = DateTimeOffset.UtcNow,
            BillingAddress = Addr(),
            ShippingAddress = Addr()
        });
        db.SaleReturns.Add(new SaleReturn
        {
            OrderId = orderId,
            ReturnStatus = status,
            RefundAmount = refundAmount,
            ReturnDate = DateTimeOffset.UtcNow,
            Source = ReturnSource.InPerson,
            IsDeleted = isDeleted,
            DeletedAt = isDeleted ? DateTimeOffset.UtcNow : default,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
    }

    private ISaleReturnManager Sut(out IServiceScope scope)
    {
        var (svc, s) = GetScopedService<ISaleReturnManager>();
        scope = s;
        return svc;
    }

    [Fact]
    public async Task EmptyDb_ReturnsAllZeros()
    {
        var sut = Sut(out var scope);
        using var _ = scope;

        var result = await sut.GetReturnKpisAsync();

        result.TotalCount.Should().Be(0);
        result.PendingCount.Should().Be(0);
        result.ApprovedCount.Should().Be(0);
        result.CompletedRefundTotal.Should().Be(0m);
    }

    [Fact]
    public async Task AggregatesCountsByStatus()
    {
        await SeedReturnAsync(ReturnStatus.Draft, 0m);
        await SeedReturnAsync(ReturnStatus.Pending, 0m);
        await SeedReturnAsync(ReturnStatus.Pending, 0m);
        await SeedReturnAsync(ReturnStatus.Approved, 0m);
        await SeedReturnAsync(ReturnStatus.Rejected, 0m);
        await SeedReturnAsync(ReturnStatus.Cancelled, 0m);
        await SeedReturnAsync(ReturnStatus.Completed, 100m);
        await SeedReturnAsync(ReturnStatus.Completed, 50m);

        var sut = Sut(out var scope);
        using var _ = scope;

        var result = await sut.GetReturnKpisAsync();

        result.TotalCount.Should().Be(8);   // tüm statüler
        result.PendingCount.Should().Be(2);
        result.ApprovedCount.Should().Be(1);
        result.CompletedRefundTotal.Should().Be(150m);
    }

    [Fact]
    public async Task CompletedRefundTotal_OnlySumsCompletedStatus()
    {
        // Approved/Pending iadelerin RefundAmount'ı dolu olsa bile toplama GİRMEMELİ.
        await SeedReturnAsync(ReturnStatus.Approved, 999m);
        await SeedReturnAsync(ReturnStatus.Pending, 500m);
        await SeedReturnAsync(ReturnStatus.Completed, 75m);

        var sut = Sut(out var scope);
        using var _ = scope;

        var result = await sut.GetReturnKpisAsync();

        result.CompletedRefundTotal.Should().Be(75m);   // sadece Completed
        result.ApprovedCount.Should().Be(1);
    }

    [Fact]
    public async Task SoftDeletedReturns_ExcludedFromAllCounts()
    {
        await SeedReturnAsync(ReturnStatus.Pending, 0m);
        await SeedReturnAsync(ReturnStatus.Completed, 100m, isDeleted: true);   // hariç
        await SeedReturnAsync(ReturnStatus.Pending, 0m, isDeleted: true);       // hariç

        var sut = Sut(out var scope);
        using var _ = scope;

        var result = await sut.GetReturnKpisAsync();

        result.TotalCount.Should().Be(1);              // sadece silinmemiş Pending
        result.PendingCount.Should().Be(1);
        result.CompletedRefundTotal.Should().Be(0m);   // silinmiş Completed sayılmaz
    }
}

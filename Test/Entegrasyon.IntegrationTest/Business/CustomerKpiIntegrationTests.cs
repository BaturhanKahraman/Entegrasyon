using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Customers;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// CustomerManager.GetCustomerKpisAsync — Müşteriler liste sayfası KPI snapshot'ı.
///
/// Tek server-side GroupBy(CustomerType, TPH discriminator) → count + IsActive sayımı.
/// "Retail"=bireysel, "Corporate"=kurumsal. Discriminator GroupBy + soft-delete query filter
/// semantiği ancak gerçek Postgres'te doğrulanabilir.
/// </summary>
[Trait("Category", "Integration")]
public class CustomerKpiIntegrationTests : IntegrationTestBase
{
    public CustomerKpiIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    private static Address Addr() => new() { City = "Istanbul", Country = "TR", FullAddress = "x" };

    private async Task SeedRetailAsync(bool isActive, bool isDeleted = false)
    {
        using var db = CreateDbContext();
        db.Customers.Add(new RetailCustomer
        {
            FullName = "Birey",
            IsActive = isActive,
            IsDeleted = isDeleted,
            DeletedAt = isDeleted ? DateTimeOffset.UtcNow : default,
            CreatedAt = DateTimeOffset.UtcNow,
            Address = Addr()
        });
        await db.SaveChangesAsync();
    }

    private async Task SeedCorporateAsync(bool isActive, bool isDeleted = false)
    {
        using var db = CreateDbContext();
        db.Customers.Add(new CorporateCustomer
        {
            FullName = "Kurum",
            IsActive = isActive,
            IsDeleted = isDeleted,
            DeletedAt = isDeleted ? DateTimeOffset.UtcNow : default,
            CreatedAt = DateTimeOffset.UtcNow,
            Address = Addr()
        });
        await db.SaveChangesAsync();
    }

    private ICustomerManager Sut(out IServiceScope scope)
    {
        var (svc, s) = GetScopedService<ICustomerManager>();
        scope = s;
        return svc;
    }

    [Fact]
    public async Task EmptyDb_ReturnsAllZeros()
    {
        var sut = Sut(out var scope);
        using var _ = scope;

        var result = await sut.GetCustomerKpisAsync();

        result.IndividualCount.Should().Be(0);
        result.CorporateCount.Should().Be(0);
        result.ActiveCount.Should().Be(0);
    }

    [Fact]
    public async Task CountsByTypeAndActive()
    {
        // Bireysel: 3 (2 aktif, 1 pasif) ; Kurumsal: 2 (1 aktif, 1 pasif)
        await SeedRetailAsync(isActive: true);
        await SeedRetailAsync(isActive: true);
        await SeedRetailAsync(isActive: false);
        await SeedCorporateAsync(isActive: true);
        await SeedCorporateAsync(isActive: false);

        var sut = Sut(out var scope);
        using var _ = scope;

        var result = await sut.GetCustomerKpisAsync();

        result.IndividualCount.Should().Be(3);
        result.CorporateCount.Should().Be(2);
        result.ActiveCount.Should().Be(3);   // 2 bireysel aktif + 1 kurumsal aktif
    }

    [Fact]
    public async Task SoftDeletedCustomers_ExcludedFromAllCounts()
    {
        await SeedRetailAsync(isActive: true);
        await SeedRetailAsync(isActive: true, isDeleted: true);    // hariç
        await SeedCorporateAsync(isActive: true, isDeleted: true); // hariç

        var sut = Sut(out var scope);
        using var _ = scope;

        var result = await sut.GetCustomerKpisAsync();

        result.IndividualCount.Should().Be(1);
        result.CorporateCount.Should().Be(0);
        result.ActiveCount.Should().Be(1);
    }
}

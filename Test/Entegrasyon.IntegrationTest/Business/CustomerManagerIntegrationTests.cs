using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity.DiscountVouchers;
using Entegrasyon.Entity.Dtos.Customers;
using Entegrasyon.Entity.Logs;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.IntegrationTest.Business;

[Trait("Category", "Integration")]
public class CustomerManagerIntegrationTests : IntegrationTestBase
{
    public CustomerManagerIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    [Fact]
    public async Task SetActive_Deactivate_ShouldPersistFields_AndWriteLog()
    {
        var (_, customerId) = await SeedCustomerAndUserAsync();
        var (manager, scope) = GetScopedService<ICustomerManager>();
        using var _ = scope;

        var result = await manager.SetActive(customerId, false, "Test: dolandırıcılık şüphesi");

        result.Success.Should().BeTrue(result.Message);

        using var db = CreateDbContext();
        var cust = await db.Customers.AsNoTracking().FirstAsync(c => c.Id == customerId);
        cust.IsActive.Should().BeFalse();
        cust.DeactivatedAt.Should().NotBeNull();
        cust.DeactivationReason.Should().Be("Test: dolandırıcılık şüphesi");

        var log = await db.Logs.AsNoTracking()
            .Where(l => l.EntityType == "Customer" && l.EntityId == customerId.ToString())
            .OrderByDescending(l => l.Id)
            .FirstOrDefaultAsync();
        log.Should().NotBeNull();
        log!.LogType.Should().Be(LogType.Customer);
        log.LogAction.Should().Be(LogAction.Update);
        log.Content.Should().Contain("deaktif");
    }

    [Fact]
    public async Task SetActive_Reactivate_ShouldClearDeactivationFields()
    {
        var (_, customerId) = await SeedCustomerAndUserAsync();
        var (manager, scope) = GetScopedService<ICustomerManager>();
        using var _ = scope;

        await manager.SetActive(customerId, false, "Deneme");
        var result = await manager.SetActive(customerId, true);

        result.Success.Should().BeTrue(result.Message);

        using var db = CreateDbContext();
        var cust = await db.Customers.AsNoTracking().FirstAsync(c => c.Id == customerId);
        cust.IsActive.Should().BeTrue();
        cust.DeactivatedAt.Should().BeNull();
        cust.DeactivationReason.Should().BeNull();
    }

    [Fact]
    public async Task SetActive_Idempotent_WhenStateAlreadyMatches_ShouldNotWriteLog()
    {
        var (_, customerId) = await SeedCustomerAndUserAsync();
        var (manager, scope) = GetScopedService<ICustomerManager>();
        using var _ = scope;

        using (var db0 = CreateDbContext())
        {
            var logsBefore = await db0.Logs
                .Where(l => l.EntityType == "Customer" && l.EntityId == customerId.ToString())
                .CountAsync();
            logsBefore.Should().Be(0);
        }

        var result = await manager.SetActive(customerId, true);

        result.Success.Should().BeTrue();

        using var db = CreateDbContext();
        var logsAfter = await db.Logs
            .Where(l => l.EntityType == "Customer" && l.EntityId == customerId.ToString())
            .CountAsync();
        logsAfter.Should().Be(0);
    }

    [Fact]
    public async Task SetActive_CustomerNotFound_ShouldReturnError()
    {
        var (manager, scope) = GetScopedService<ICustomerManager>();
        using var _ = scope;

        var result = await manager.SetActive(999_999, false);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetCustomerActivity_ShouldReturn_LifecycleAndLogAndVoucherEvents_OrderedByDateDesc()
    {
        var (_, customerId) = await SeedCustomerAndUserAsync();

        using (var db = CreateDbContext())
        {
            db.DiscountVouchers.Add(new DiscountVoucher
            {
                CustomerId = customerId,
                Code = "WELCOME10",
                Amount = 10m,
                DiscountType = DiscountType.FixedAmount,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-30)
            });
            await db.SaveChangesAsync();
        }

        var (manager, scope) = GetScopedService<ICustomerManager>();
        using var _ = scope;

        await manager.SetActive(customerId, false, "Test deaktivasyon");

        var result = await manager.GetCustomerActivity(customerId);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNullOrEmpty();

        result.Data!.Any(a => a.Source == CustomerActivitySource.Lifecycle
                              && a.Title.Contains("oluşturuldu")).Should().BeTrue();
        result.Data.Any(a => a.Source == CustomerActivitySource.Lifecycle
                             && a.Title.Contains("deaktif")).Should().BeTrue();
        result.Data.Any(a => a.Source == CustomerActivitySource.DiscountVoucher
                             && (a.Title.Contains("WELCOME10") || a.Title.Contains("Kupon"))).Should().BeTrue();
        result.Data.Any(a => a.Source == CustomerActivitySource.ApplicationLog).Should().BeTrue();

        result.Data.Select(a => a.OccurredAt).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task GetCustomerActivity_CustomerNotFound_ShouldReturnError()
    {
        var (manager, scope) = GetScopedService<ICustomerManager>();
        using var _ = scope;

        var result = await manager.GetCustomerActivity(999_999);

        result.Success.Should().BeFalse();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task AddCustomer_ShouldWrite_CustomerEntityLog()
    {
        var (manager, scope) = GetScopedService<ICustomerManager>();
        using var _ = scope;

        var dto = new CustomerAddDto(
            NationalIdentity: "12345678901",
            TaxNumber: "",
            Name: "Unit",
            Surname: "Test",
            CorporateName: "",
            PhoneNumber: "5551234567",
            FullAddress: "Test Adres",
            CustomerType: "Retail");

        var result = await manager.AddCustomer(dto);
        result.Success.Should().BeTrue(result.Message);

        using var db = CreateDbContext();
        var created = await db.Customers.AsNoTracking()
            .Where(c => c.FullName!.Contains("Unit"))
            .FirstOrDefaultAsync();
        created.Should().NotBeNull();

        var entityLog = await db.Logs.AsNoTracking()
            .Where(l => l.EntityType == "Customer"
                        && l.EntityId == created!.Id.ToString()
                        && l.LogAction == LogAction.Add)
            .FirstOrDefaultAsync();
        entityLog.Should().NotBeNull();
    }
}

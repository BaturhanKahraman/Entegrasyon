using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Customers;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Orders;
using Entegrasyon.Entity.Sales;
using Entegrasyon.IntegrationTest.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Sales;

[Trait("Category", "Integration")]
public class UnifiedSalesViewTests : IntegrationTestBase
{
    public UnifiedSalesViewTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    [Fact]
    public async Task GetPageableAsync_ReturnsBothSaleAndOrder_InSameList()
    {
        // Arrange
        await SeedBasicEntitiesAsync();
        var (userId, _) = await SeedCustomerAndUserAsync();

        using (var db = CreateDbContext())
        {
            db.Sales.Add(new Sale
            {
                Id = Guid.NewGuid(),
                SaleNumber = "POS-TEST-1",
                SaleDate = DateTimeOffset.UtcNow.AddHours(-1),
                SaleSource = SaleSource.POS,
                SaleStatus = SaleStatus.Completed,
                SalePersonId = userId,
                BranchOfficeId = 1,
                GeneralDiscount = 0
            });

            db.Orders.Add(new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = "TY-TEST-1",
                OrderDate = DateTimeOffset.UtcNow.AddMinutes(-30),
                MarketPlaceId = 1,
                CustomerFirstName = "Ali",
                CustomerLastName = "Veli",
                BillingAddress = new Address { City = "Istanbul", Country = "TR", FullAddress = "x" },
                ShippingAddress = new Address { City = "Istanbul", Country = "TR", FullAddress = "y" }
            });

            await db.SaveChangesAsync();
        }

        // Act
        var (manager, scope) = GetScopedService<IUnifiedSaleManager>();
        using var _ = scope;

        var filter = new UnifiedSaleFilterDto(
            Source: null, Status: null,
            StartDate: DateTimeOffset.UtcNow.AddDays(-1),
            EndDate: DateTimeOffset.UtcNow.AddDays(1),
            SearchText: null, CustomerId: null,
            PageIndex: 0, PageSize: 25);

        var result = await manager.GetPageableAsync(filter);

        // Assert
        result.Success.Should().BeTrue(result.Message);
        result.Data!.Items.Should().HaveCountGreaterThanOrEqualTo(2);
        result.Data.Items.Should().Contain(x => x.Number == "POS-TEST-1" && x.Source == UnifiedSaleSource.POS);
        result.Data.Items.Should().Contain(x => x.Number == "TY-TEST-1" && x.Source == UnifiedSaleSource.Trendyol);
    }

    [Fact]
    public async Task GetPageableAsync_FiltersBySource_WhenSourceProvided()
    {
        // Arrange
        await SeedBasicEntitiesAsync();
        var (userId, _) = await SeedCustomerAndUserAsync();

        using (var db = CreateDbContext())
        {
            db.Sales.Add(new Sale
            {
                Id = Guid.NewGuid(),
                SaleNumber = "POS-FILTER-1",
                SaleDate = DateTimeOffset.UtcNow,
                SaleSource = SaleSource.POS,
                SaleStatus = SaleStatus.Completed,
                SalePersonId = userId,
                BranchOfficeId = 1
            });
            db.Orders.Add(new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = "TY-FILTER-1",
                OrderDate = DateTimeOffset.UtcNow,
                MarketPlaceId = 1,
                BillingAddress = new Address { City = "Istanbul", Country = "TR", FullAddress = "x" },
                ShippingAddress = new Address { City = "Istanbul", Country = "TR", FullAddress = "y" }
            });
            await db.SaveChangesAsync();
        }

        var (manager, scope) = GetScopedService<IUnifiedSaleManager>();
        using var _ = scope;

        var filter = new UnifiedSaleFilterDto(
            Source: UnifiedSaleSource.POS, Status: null,
            StartDate: DateTimeOffset.UtcNow.AddDays(-1),
            EndDate: DateTimeOffset.UtcNow.AddDays(1),
            SearchText: null, CustomerId: null,
            PageIndex: 0, PageSize: 25);

        // Act
        var result = await manager.GetPageableAsync(filter);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Items.Should().OnlyContain(x => x.Source == UnifiedSaleSource.POS);
        result.Data.Items.Should().Contain(x => x.Number == "POS-FILTER-1");
    }

    [Fact]
    public async Task GetSourceCountsAsync_ReturnsTumuPlusIndividualSources()
    {
        // Arrange
        await SeedBasicEntitiesAsync();
        var (userId, _) = await SeedCustomerAndUserAsync();

        using (var db = CreateDbContext())
        {
            db.Sales.Add(new Sale
            {
                Id = Guid.NewGuid(),
                SaleNumber = "POS-COUNT-1",
                SaleDate = DateTimeOffset.UtcNow,
                SaleSource = SaleSource.POS,
                SaleStatus = SaleStatus.Completed,
                SalePersonId = userId,
                BranchOfficeId = 1
            });
            await db.SaveChangesAsync();
        }

        var (manager, scope) = GetScopedService<IUnifiedSaleManager>();
        using var _ = scope;

        var filter = new UnifiedSaleFilterDto(
            Source: null, Status: null,
            StartDate: DateTimeOffset.UtcNow.AddDays(-1),
            EndDate: DateTimeOffset.UtcNow.AddDays(1),
            SearchText: null, CustomerId: null);

        // Act
        var result = await manager.GetSourceCountsAsync(filter);

        // Assert
        result.Success.Should().BeTrue();
        result.Data!.Should().Contain(x => x.Source == null && x.Label == "Tümü");
        result.Data!.Should().Contain(x => x.Source == UnifiedSaleSource.POS && x.Count >= 1);
    }
}

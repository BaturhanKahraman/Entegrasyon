using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Orders;
using Entegrasyon.IntegrationTest.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// #19 EF no-tracking persist footgun regresyon testleri.
/// Global default NoTracking (TenantDbContextFactory) → bir entity LINQ ile (FirstOrDefaultAsync)
/// AsTracking OLMADAN yüklenip mutate edilir + SaveChangesAsync çağrılırsa değişiklik SESSİZCE
/// persist ETMEZ. Bu testler manager'ın gerçekten DB'ye yazdığını doğrular (RED-first: .AsTracking()
/// fix'inden ÖNCE kırmızı, sonra yeşil).
/// NOT: Testcontainers/PostgreSQL gerektirir.
/// </summary>
[Trait("Category", "Integration")]
public sealed class NoTrackingPersistRegressionTests : IntegrationTestBase
{
    public NoTrackingPersistRegressionTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    [Fact]
    public async Task MarketPlaceManager_UpdateCredentials_persists_to_db()
    {
        using (var db = CreateDbContext())
        {
            if (!await db.MarketPlaces.AnyAsync(m => m.Id == 1))
                db.MarketPlaces.Add(new MarketPlace { Id = 1, Name = "Trendyol", CreatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
        }

        var manager = GetService<IMarketPlaceManager>();
        var result = await manager.UpdateCredentialsAsync(1, "API-KEY-XYZ", "API-SECRET-XYZ", "SELLER-42", baseUrl: null);
        result.Success.Should().BeTrue();

        using var verify = CreateDbContext();
        var mp = await verify.MarketPlaces.AsNoTracking().FirstAsync(m => m.Id == 1);
        mp.ApiKey.Should().Be("API-KEY-XYZ", "kimlik bilgileri DB'ye yazılmalı (no-tracking sessiz no-op olmamalı)");
        mp.ApiSecret.Should().Be("API-SECRET-XYZ");
        mp.SellerId.Should().Be("SELLER-42");
    }

    [Fact]
    public async Task OrderManager_UpdateOrderStatus_persists_to_db()
    {
        var orderId = Guid.NewGuid();
        using (var db = CreateDbContext())
        {
            db.Orders.Add(new Order
            {
                Id = orderId,
                OrderNumber = "TY-PERSIST-1",
                OrderDate = DateTimeOffset.UtcNow,
                MarketplaceOrderStatus = "Created",
                BillingAddress = new Address { City = "Ankara", Country = "TR", FullAddress = "x" },
                ShippingAddress = new Address { City = "Ankara", Country = "TR", FullAddress = "y" }
            });
            await db.SaveChangesAsync();
        }

        var manager = GetService<IOrderManager>();
        var result = await manager.UpdateOrderStatusAsync(orderId, "Shipped");
        result.Success.Should().BeTrue();

        using var verify = CreateDbContext();
        var order = await verify.Orders.AsNoTracking().FirstAsync(o => o.Id == orderId);
        order.MarketplaceOrderStatus.Should().Be("Shipped", "sipariş durumu DB'ye yazılmalı");
    }
}

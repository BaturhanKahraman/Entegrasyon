using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Orders;
using Entegrasyon.IntegrationTest.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.IntegrationTest.Business;

/// <summary>
/// OrderManager.GetPickingKpisAsync — Sipariş Hazırlama (Picking) sayfası KPI aggregate'leri.
///
/// 3 read-path sayım (MarketplaceOrderStatus="Created" filtresi):
///  - PendingItemCount  = Σ TotalQuantity (bekleyen siparişlerin ürün adedi)
///  - TodayOrderCount   = bugün (UTC) gelen sipariş sayısı
///  - StaleOrderCount   = 24 saatten uzun bekleyen sipariş sayısı
/// GroupBy/Sum/koşullu Count ve soft-delete query filter semantiği ancak gerçek Postgres'te doğrulanır.
/// </summary>
[Trait("Category", "Integration")]
public class PickingKpiIntegrationTests : IntegrationTestBase
{
    public PickingKpiIntegrationTests(PostgreSqlFixture pgFixture, WireMockFixture wireMock)
        : base(pgFixture, wireMock) { }

    /// <summary>
    /// Order seed eder. NOT: <c>Order.TotalQuantity</c> EF Core'da <c>HasDefaultValue(0)</c> ile
    /// yapılandırıldığından INSERT sırasında verilen değer 0'a düşüyor; bu yüzden gerçek değer
    /// INSERT'ten sonra raw UPDATE (ExecuteUpdate) ile yazılır — prod'da bu alan OrderItems'tan
    /// hesaplanır, test gerçekçi bir adet üretir.
    /// </summary>
    private async Task SeedOrderAsync(string status, DateTimeOffset orderDate, int totalQuantity)
    {
        var id = Guid.NewGuid();
        using (var db = CreateDbContext())
        {
            db.Orders.Add(new Order
            {
                Id = id,
                OrderNumber = $"ORD-{Guid.NewGuid():N}".Substring(0, 12),
                MarketplaceOrderStatus = status,
                OrderDate = orderDate,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }

        using (var db = CreateDbContext())
        {
            await db.Orders.Where(o => o.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.TotalQuantity, totalQuantity));
        }
    }

    [Fact]
    public async Task GetPickingKpisAsync_SadeceCreated_DogruSayar()
    {
        var now = DateTimeOffset.UtcNow;
        // Bugün gelen 2 Created sipariş (qty 3 + 2)
        await SeedOrderAsync("Created", now, 3);
        await SeedOrderAsync("Created", now, 2);
        // 25 saat önce gelen 1 Created sipariş (stale, qty 1)
        await SeedOrderAsync("Created", now.AddHours(-25), 1);
        // Farklı statü — KPI'a dahil olmamalı
        await SeedOrderAsync("Picking", now, 10);

        var sut = GetService<IOrderManager>();
        var kpi = await sut.GetPickingKpisAsync();

        Assert.Equal(6, kpi.PendingItemCount);   // 3 + 2 + 1 (Picking hariç)
        Assert.Equal(2, kpi.TodayOrderCount);     // bugünkü 2 Created
        Assert.Equal(1, kpi.StaleOrderCount);     // 25 saatlik 1 Created
    }

    [Fact]
    public async Task GetPickingKpisAsync_HicBekleyenYok_SifirDoner()
    {
        await SeedOrderAsync("Picking", DateTimeOffset.UtcNow, 5);

        var sut = GetService<IOrderManager>();
        var kpi = await sut.GetPickingKpisAsync();

        Assert.Equal(0, kpi.PendingItemCount);
        Assert.Equal(0, kpi.TodayOrderCount);
        Assert.Equal(0, kpi.StaleOrderCount);
    }
}

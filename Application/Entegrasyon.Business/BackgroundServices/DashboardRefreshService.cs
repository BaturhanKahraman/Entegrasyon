using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Periyodik olarak mv_product_stock_summary materialized view'ını refresh eder
/// ve dashboard cache'ini ısıtır. 3 dakikada bir çalışır.
/// </summary>
public class DashboardRefreshService(
    IServiceScopeFactory scopeFactory,
    ILogger<DashboardRefreshService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Başlangıçta ısıt
        await RefreshAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(3));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RefreshAsync(stoppingToken);
        }
    }

    private async Task RefreshAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();

            // Materialized view'ı refresh et
            var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
            await using var db = await dbFactory.CreateDbContextAsync(ct);
            await db.Database.ExecuteSqlRawAsync(
                "REFRESH MATERIALIZED VIEW CONCURRENTLY mv_product_stock_summary", ct);

            // Cache'i ısıt
            var dashboard = scope.ServiceProvider.GetRequiredService<IDashboardManager>();
            await dashboard.GetStatsAsync();
            await dashboard.GetWeeklySalesAsync();
            await dashboard.GetMarketplaceStatusesAsync();
            await dashboard.GetRecentActivitiesAsync();

            logger.LogDebug("Dashboard cache refreshed successfully");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Dashboard refresh failed, will retry in next cycle");
        }
    }
}

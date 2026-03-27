using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Tenants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Periyodik olarak mv_product_stock_summary materialized view'ını refresh eder
/// ve dashboard cache'ini ısıtır. 3 dakikada bir, her tenant için çalışır.
/// </summary>
public class DashboardRefreshService(
    IServiceScopeFactory scopeFactory,
    ITenantRegistry tenantRegistry,
    ILogger<DashboardRefreshService> logger)
    : TenantAwarePollingService(scopeFactory, tenantRegistry, logger)
{
    protected override TimeSpan PollInterval => TimeSpan.FromMinutes(3);

    /// <summary>Tüm tenantlar için çalışır — feature kontrolü yok.</summary>
    protected override string? RequiredFeature => null;

    protected override async Task PollForTenantAsync(
        IServiceProvider services, int tenantId,
        DateTimeOffset lastPoll, CancellationToken ct)
    {
        // Materialized view'ı refresh et
        var dbFactory = services.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await db.Database.ExecuteSqlRawAsync(
            "REFRESH MATERIALIZED VIEW CONCURRENTLY mv_product_stock_summary", ct);

        // Cache'i ısıt
        var dashboard = services.GetRequiredService<IDashboardManager>();
        await dashboard.GetStatsAsync();
        await dashboard.GetWeeklySalesAsync();
        await dashboard.GetMarketplaceStatusesAsync();
        await dashboard.GetRecentActivitiesAsync();

        logger.LogDebug("Dashboard cache refreshed successfully for tenant {TenantId}", tenantId);
    }
}

using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Terk edilen sepetleri periyodik olarak tarar ve kurtarma emailleri gonderir.
/// 15 dakikada bir calisir.
/// </summary>
public class AbandonedCartBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<AbandonedCartBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait 5 minutes before first run
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessAsync(stoppingToken);
        }
    }

    private async Task ProcessAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            // Get all tenants with abandoned cart recovery enabled
            var tenantIds = await db.StorefrontSettings
                .Where(s => s.AbandonedCartRecoveryEnabled && !s.IsDeleted)
                .Select(s => s.TenantId)
                .ToListAsync(ct);

            foreach (var tenantId in tenantIds)
            {
                try
                {
                    var abandonedCartManager = scope.ServiceProvider.GetRequiredService<IStorefrontAbandonedCartManager>();
                    await abandonedCartManager.ProcessAbandonedCartsAsync(tenantId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Tenant {TenantId} icin terk edilen sepet isleme hatasi", tenantId);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Terk edilen sepet background servisi hatasi");
        }
    }
}

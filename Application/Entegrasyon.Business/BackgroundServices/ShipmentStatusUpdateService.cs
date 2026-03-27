using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Tenants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Shipping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Periyodik olarak terminal olmayan kargolarin durumunu gunceller.
/// 30 dakikada bir calisir, batch halinde (50'li gruplar) isler.
/// </summary>
public class ShipmentStatusUpdateService(
    IServiceScopeFactory scopeFactory,
    ITenantRegistry tenantRegistry,
    ILogger<ShipmentStatusUpdateService> logger)
    : TenantAwarePollingService(scopeFactory, tenantRegistry, logger)
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private static readonly ShipmentStatus[] TerminalStatuses =
    {
        ShipmentStatus.Delivered,
        ShipmentStatus.Cancelled,
        ShipmentStatus.ReturnedToSender
    };

    private const int BatchSize = 50;

    protected override TimeSpan PollInterval => TimeSpan.FromMinutes(30);

    protected override string? RequiredFeature => "Permissions.Cargo.View";

    protected override async Task PollForTenantAsync(
        IServiceProvider services, int tenantId,
        DateTimeOffset lastPoll, CancellationToken ct)
    {
        var dbFactory = services.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var pendingIds = await db.ShipmentTrackings
            .Where(x => !TerminalStatuses.Contains(x.CurrentStatus))
            .OrderBy(x => x.LastStatusUpdate)
            .Select(x => x.Id)
            .ToListAsync(ct);

        logger.LogInformation("Kargo durum guncelleme, tenant {TenantId}: {Count} gonderi islenecek",
            tenantId, pendingIds.Count);

        foreach (var batch in pendingIds.Chunk(BatchSize))
        {
            foreach (var id in batch)
            {
                try
                {
                    // Her refresh icin yeni scope olustur
                    await using var refreshScope = _scopeFactory.CreateAsyncScope();

                    // Tenant context'i yeni scope icin de initialize et
                    var tenantContext = refreshScope.ServiceProvider.GetRequiredService<ITenantContext>();
                    var registry = refreshScope.ServiceProvider.GetRequiredService<ITenantRegistry>();
                    var tenant = await registry.GetByIdAsync(tenantId);
                    if (tenant is not null)
                        tenantContext.Initialize(tenant);

                    var manager = refreshScope.ServiceProvider.GetRequiredService<IShipmentTrackingManager>();
                    await manager.RefreshTrackingStatusAsync(id);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "Kargo durum guncelleme basarisiz, tenant {TenantId}: ShipmentTrackingId={Id}",
                        tenantId, id);
                }
            }

            // Batch arasi kisa bekleme
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
        }

        logger.LogDebug("Kargo durum guncelleme tamamlandi, tenant {TenantId}", tenantId);
    }
}

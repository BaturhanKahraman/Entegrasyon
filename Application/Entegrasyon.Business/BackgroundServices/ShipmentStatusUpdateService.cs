using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Shipping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Periyodik olarak terminal olmayan kargolarin durumunu gunceller.
/// 30 dakikada bir calisir, batch halinde (50'li gruplar) isler.
/// </summary>
public class ShipmentStatusUpdateService(
    IServiceScopeFactory scopeFactory,
    ILogger<ShipmentStatusUpdateService> logger) : BackgroundService
{
    private static readonly ShipmentStatus[] TerminalStatuses =
    {
        ShipmentStatus.Delivered,
        ShipmentStatus.Cancelled,
        ShipmentStatus.ReturnedToSender
    };

    private const int BatchSize = 50;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Baslangicta 5 dakika bekle (diger servisler hazir olsun)
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(30));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RefreshAllAsync(stoppingToken);
        }
    }

    private async Task RefreshAllAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
            await using var db = await dbFactory.CreateDbContextAsync(ct);

            var pendingIds = await db.ShipmentTrackings
                .Where(x => !TerminalStatuses.Contains(x.CurrentStatus))
                .OrderBy(x => x.LastStatusUpdate)
                .Select(x => x.Id)
                .ToListAsync(ct);

            logger.LogInformation("Kargo durum guncelleme: {Count} gonderi islenecek", pendingIds.Count);

            foreach (var batch in pendingIds.Chunk(BatchSize))
            {
                foreach (var id in batch)
                {
                    try
                    {
                        // Her refresh icin yeni scope olustur
                        await using var refreshScope = scopeFactory.CreateAsyncScope();
                        var manager = refreshScope.ServiceProvider.GetRequiredService<IShipmentTrackingManager>();
                        await manager.RefreshTrackingStatusAsync(id);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        logger.LogWarning(ex, "Kargo durum guncelleme basarisiz: ShipmentTrackingId={Id}", id);
                    }
                }

                // Batch arasi kisa bekleme
                await Task.Delay(TimeSpan.FromSeconds(2), ct);
            }

            logger.LogDebug("Kargo durum guncelleme tamamlandi");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Kargo durum guncelleme dongusu basarisiz, sonraki cevrimde tekrar denenecek");
        }
    }
}

using System.Collections.Concurrent;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Her 30 saniyede PttAVM tracking sonuclarini kontrol eder.
/// Pending/InProgress kayitlari sorgular, durumu gunceller.
/// Multi-tenant hazir: ConcurrentDictionary ile tenant basina son poll zamani takip edilir.
/// </summary>
public class PttavmProductTrackingPollingService(
    IServiceScopeFactory scopeFactory,
    ILogger<PttavmProductTrackingPollingService> logger) : BackgroundService
{
    private const int PttavmMarketPlaceId = MarketPlaceConstants.PttavmMarketPlaceId;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan TrackingTimeout = TimeSpan.FromHours(24);

    // Multi-tenant: tenant basina son poll zamani (key = tenantId)
    private readonly ConcurrentDictionary<int, DateTime> _lastPollTime = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollPendingTrackingsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "PttAVM product tracking polling hatası");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task PollPendingTrackingsAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();
        var productService = scope.ServiceProvider.GetRequiredService<IPttavmProductService>();
        var activityLogger = scope.ServiceProvider.GetRequiredService<IProductActivityLogger>();

        var pendingRecords = await dbContext.ProductMarketplaces
            .Where(pm => pm.MarketPlaceId == PttavmMarketPlaceId &&
                         pm.Status == MarketplaceProductStatus.Pending &&
                         pm.BatchRequestId != null)
            .ToListAsync(ct);

        if (pendingRecords.Count == 0) return;

        logger.LogInformation("PttAVM polling: {Count} pending tracking requests", pendingRecords.Count);

        foreach (var record in pendingRecords)
        {
            try
            {
                // Timeout kontrolu
                if (DateTimeOffset.UtcNow - record.UpdatedAt > TrackingTimeout)
                {
                    record.Status = MarketplaceProductStatus.Failed;
                    record.StatusMessage = "PttAVM tracking işlemi 24 saat içinde tamamlanmadı — zaman aşımı.";
                    logger.LogWarning("PttAVM tracking {TrackingId} timed out for ProductId={ProductId}",
                        record.BatchRequestId, record.ProductId);
                    await activityLogger.LogAsync(record.ProductId, ProductActivityType.BatchFailed,
                        "PttAVM tracking zaman aşımına uğradı (24 saat)", ProductActivityStatus.Error,
                        marketplaceName: "PttAVM", referenceId: record.BatchRequestId);
                    continue;
                }

                var result = await productService.GetTrackingResultAsync(record.BatchRequestId!, ct);
                if (!result.Success || result.Data is null)
                {
                    logger.LogWarning(
                        "PttAVM tracking check failed for {TrackingId}, ProductId={ProductId}. Will retry next cycle.",
                        record.BatchRequestId, record.ProductId);
                    continue;
                }

                var trackingData = result.Data;
                var subResult = trackingData.ProductsSubTrackingResult;

                if (subResult is null)
                {
                    logger.LogDebug("PttAVM tracking {TrackingId} has no sub-result yet, will retry",
                        record.BatchRequestId);
                    continue;
                }

                // Hala isleniyor
                if (subResult.CountOfInProgressProducts > 0 || subResult.CountOfWaitingProducts > 0)
                {
                    logger.LogDebug("PttAVM tracking {TrackingId} still in progress, will retry",
                        record.BatchRequestId);
                    continue;
                }

                // Iptal edilen urunler var mi
                if (subResult.CountOfCancelledProducts > 0)
                {
                    var failedInfos = subResult.ProductBasedInfos?
                        .Where(p => p.Status is "Cancelled")
                        .SelectMany(p => p.FailureReasons ?? new List<string>())
                        .ToList() ?? new List<string>();

                    record.Status = MarketplaceProductStatus.Failed;
                    record.StatusMessage = failedInfos.Count > 0
                        ? string.Join("; ", failedInfos)
                        : "PttAVM tracking işlemi iptal edildi.";
                    logger.LogWarning("PttAVM tracking {TrackingId} has cancelled products: {Message}",
                        record.BatchRequestId, record.StatusMessage);
                    await activityLogger.LogAsync(record.ProductId, ProductActivityType.BatchFailed,
                        $"PttAVM tracking başarısız: {record.StatusMessage}",
                        ProductActivityStatus.Error, marketplaceName: "PttAVM",
                        referenceId: record.BatchRequestId);
                }
                else
                {
                    record.Status = MarketplaceProductStatus.Published;
                    record.LastSyncedAt = DateTimeOffset.UtcNow;
                    record.StatusMessage = null;
                    logger.LogInformation("PttAVM tracking {TrackingId} completed — ProductId={ProductId} published",
                        record.BatchRequestId, record.ProductId);
                    await activityLogger.LogAsync(record.ProductId, ProductActivityType.BatchCompleted,
                        "PttAVM tracking tamamlandı — ürün yayında",
                        ProductActivityStatus.Success, marketplaceName: "PttAVM",
                        referenceId: record.BatchRequestId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to check PttAVM tracking for {TrackingId}", record.BatchRequestId);
            }
        }

        await dbContext.SaveChangesAsync(ct);
        _lastPollTime[0] = DateTime.UtcNow; // tenantId=0 simdilik tek tenant
    }
}

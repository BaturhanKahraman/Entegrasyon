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
/// Periyodik olarak Çiçeksepeti'nde bekleyen batch işlemlerini kontrol eder.
/// Ürün batch'leri 24 saat içinde tamamlanmazsa Failed olarak işaretlenir.
/// Multi-tenant hazır: ConcurrentDictionary ile tenant başına son poll zamanı takip edilir.
/// </summary>
public class CiceksepetiBatchStatusPollingService(
    IServiceScopeFactory scopeFactory,
    ILogger<CiceksepetiBatchStatusPollingService> logger) : BackgroundService
{
    private const int CiceksepetiMarketPlaceId = MarketPlaceConstants.CiceksepetiMarketPlaceId;

    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ProductBatchTimeout = TimeSpan.FromHours(24);

    // Multi-tenant: tenant başına son poll zamanı (key = tenantId)
    private readonly ConcurrentDictionary<int, DateTime> _lastPollTime = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // İlk başlatmada kısa bir bekleme — uygulama tam açılsın
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollPendingBatchesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Çiçeksepeti batch status polling failed");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task PollPendingBatchesAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();
        var productService = scope.ServiceProvider.GetRequiredService<ICiceksepetiProductService>();
        var activityLogger = scope.ServiceProvider.GetRequiredService<IProductActivityLogger>();

        var pendingRecords = await dbContext.ProductMarketplaces
            .Where(pm => pm.MarketPlaceId == CiceksepetiMarketPlaceId &&
                         pm.Status == MarketplaceProductStatus.Pending &&
                         pm.BatchRequestId != null)
            .ToListAsync(ct);

        if (pendingRecords.Count == 0) return;

        logger.LogInformation("Çiçeksepeti polling: {Count} pending batch requests", pendingRecords.Count);

        foreach (var record in pendingRecords)
        {
            try
            {
                // Timeout kontrolü — 24 saat içinde tamamlanmadıysa Failed'a al
                if (DateTimeOffset.UtcNow - record.UpdatedAt > ProductBatchTimeout)
                {
                    record.Status = MarketplaceProductStatus.Failed;
                    record.StatusMessage = "Çiçeksepeti batch işlemi 24 saat içinde tamamlanmadı — zaman aşımı.";
                    logger.LogWarning("Çiçeksepeti batch {BatchId} timed out for ProductId={ProductId}",
                        record.BatchRequestId, record.ProductId);
                    await activityLogger.LogAsync(record.ProductId, ProductActivityType.BatchFailed,
                        "Çiçeksepeti batch zaman aşımına uğradı (24 saat)", ProductActivityStatus.Error,
                        marketplaceName: "Çiçeksepeti", referenceId: record.BatchRequestId);
                    continue;
                }

                var result = await productService.CheckBatchStatusAsync(record.BatchRequestId!, ct);
                if (!result.Success || result.Data is null)
                {
                    logger.LogWarning(
                        "Çiçeksepeti batch status check failed for {BatchId}, ProductId={ProductId}. Will retry next cycle.",
                        record.BatchRequestId, record.ProductId);
                    continue;
                }

                var batchItems = result.Data.Items ?? [];
                var failedItems = batchItems
                    .Where(i => i.FailureReasons is { Count: > 0 })
                    .ToList();

                // Tüm itemlar tamamlandıysa (hiç in-progress yoksa) durumu güncelle
                var inProgressItems = batchItems.Where(i => i.Status is "IN_PROGRESS" or "PROCESSING").ToList();
                if (inProgressItems.Count > 0)
                {
                    // Hâlâ işleniyor — sonraki döngüde tekrar kontrol
                    logger.LogDebug("Çiçeksepeti batch {BatchId} still in progress ({Count} items), will retry",
                        record.BatchRequestId, inProgressItems.Count);
                    continue;
                }

                if (failedItems.Count > 0)
                {
                    var reasons = failedItems
                        .SelectMany(i => i.FailureReasons!)
                        .Select(r => r.Message)
                        .ToList();
                    record.Status = MarketplaceProductStatus.Failed;
                    record.StatusMessage = reasons.Count > 0
                        ? string.Join("; ", reasons)
                        : "Çiçeksepeti batch işlemi başarısız.";
                    logger.LogWarning("Çiçeksepeti batch {BatchId} has failures — ProductId={ProductId}: {Message}",
                        record.BatchRequestId, record.ProductId, record.StatusMessage);
                    await activityLogger.LogAsync(record.ProductId, ProductActivityType.BatchFailed,
                        $"Çiçeksepeti batch başarısız: {record.StatusMessage}",
                        ProductActivityStatus.Error, marketplaceName: "Çiçeksepeti",
                        referenceId: record.BatchRequestId);
                }
                else
                {
                    record.Status = MarketplaceProductStatus.Published;
                    record.LastSyncedAt = DateTimeOffset.UtcNow;
                    record.StatusMessage = null;
                    logger.LogInformation("Çiçeksepeti batch {BatchId} completed — ProductId={ProductId} published",
                        record.BatchRequestId, record.ProductId);
                    await activityLogger.LogAsync(record.ProductId, ProductActivityType.BatchCompleted,
                        "Çiçeksepeti batch tamamlandı — ürün yayında",
                        ProductActivityStatus.Success, marketplaceName: "Çiçeksepeti",
                        referenceId: record.BatchRequestId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to check Çiçeksepeti batch status for {BatchId}", record.BatchRequestId);
            }
        }

        await dbContext.SaveChangesAsync(ct);
        _lastPollTime[0] = DateTime.UtcNow; // tenantId=0 şimdilik tek tenant
    }
}

using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Pazarama;
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
/// Pazarama ürün batch durumu periyodik polling servisi.
/// Pending + BatchRequestId olan ProductMarketplace kayıtlarını izler.
/// Status: 1=InProgress (bekle), 2=Done (Published/Failed), 3=Error (Failed).
/// </summary>
public class PazaramaBatchStatusPollingService(
    IServiceScopeFactory scopeFactory,
    ILogger<PazaramaBatchStatusPollingService> logger) : BackgroundService
{
    private const int PazaramaMarketPlaceId = MarketPlaceConstants.PazaramaMarketPlaceId;
    private const int StatusInProgress = 1;
    private const int StatusDone = 2;
    private const int StatusError = 3;

    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan BatchTimeout = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // İlk başlatmada kısa bir bekleme — uygulama tam açılsın
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollPendingBatchesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Pazarama batch status polling failed");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task PollPendingBatchesAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();
        var pazaramaService = scope.ServiceProvider.GetRequiredService<IPazaramaProductService>();
        var activityLogger = scope.ServiceProvider.GetRequiredService<IProductActivityLogger>();

        var pendingRecords = await dbContext.ProductMarketplaces
            .Where(pm => pm.MarketPlaceId == PazaramaMarketPlaceId &&
                         pm.Status == MarketplaceProductStatus.Pending &&
                         pm.BatchRequestId != null)
            .ToListAsync(ct);

        if (pendingRecords.Count == 0) return;

        logger.LogInformation("Pazarama polling: {Count} pending batch requests", pendingRecords.Count);

        foreach (var record in pendingRecords)
        {
            try
            {
                // Timeout kontrolü — 24 saat içinde tamamlanmadıysa Failed'a al
                if (DateTimeOffset.UtcNow - record.UpdatedAt > BatchTimeout)
                {
                    record.Status = MarketplaceProductStatus.Failed;
                    record.StatusMessage = "Pazarama batch işlemi 24 saat içinde tamamlanmadı — zaman aşımı.";
                    logger.LogWarning("Pazarama batch {BatchId} timed out for ProductId={ProductId}",
                        record.BatchRequestId, record.ProductId);
                    await activityLogger.LogAsync(record.ProductId, ProductActivityType.BatchFailed,
                        "Pazarama batch zaman aşımına uğradı (24 saat)", ProductActivityStatus.Error,
                        marketplaceName: "Pazarama", referenceId: record.BatchRequestId);
                    continue;
                }

                var result = await pazaramaService.CheckBatchStatusAsync(record.BatchRequestId!);
                if (!result.Success || result.Data is null)
                {
                    logger.LogWarning(
                        "Pazarama batch status check failed for {BatchId}, ProductId={ProductId}. Will retry next cycle.",
                        record.BatchRequestId, record.ProductId);
                    continue;
                }

                await ProcessBatchResultAsync(record, result.Data, activityLogger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to check Pazarama batch status for {BatchId}", record.BatchRequestId);
            }
        }

        await dbContext.SaveChangesAsync(ct);
    }

    private async Task ProcessBatchResultAsync(
        ProductMarketplace record,
        PazaramaBatchStatusResponse batchStatus,
        IProductActivityLogger activityLogger)
    {
        switch (batchStatus.Status)
        {
            case StatusInProgress:
                // Henüz işleniyor — sonraki döngüde tekrar kontrol
                logger.LogDebug("Pazarama batch {BatchId} still in progress, will retry",
                    record.BatchRequestId);
                break;

            case StatusDone:
                if (batchStatus.FailedCount == 0)
                {
                    record.Status = MarketplaceProductStatus.Published;
                    record.LastSyncedAt = DateTimeOffset.UtcNow;
                    record.StatusMessage = null;
                    logger.LogInformation("Pazarama batch {BatchId} completed — ProductId={ProductId} published",
                        record.BatchRequestId, record.ProductId);

                    await activityLogger.LogAsync(record.ProductId, ProductActivityType.BatchCompleted,
                        "Pazarama batch tamamlandı — ürün yayında",
                        ProductActivityStatus.Success, marketplaceName: "Pazarama",
                        referenceId: record.BatchRequestId);
                }
                else
                {
                    var errorMessages = batchStatus.FailedProducts is { Count: > 0 }
                        ? string.Join("; ", batchStatus.FailedProducts.Select(fp => fp.ErrorReason))
                        : "Pazarama batch işlemi başarısız.";

                    record.Status = MarketplaceProductStatus.Failed;
                    record.StatusMessage = errorMessages;
                    logger.LogWarning("Pazarama batch {BatchId} has failures — ProductId={ProductId}: {Message}",
                        record.BatchRequestId, record.ProductId, errorMessages);

                    await activityLogger.LogAsync(record.ProductId, ProductActivityType.BatchFailed,
                        $"Pazarama batch başarısız: {errorMessages}",
                        ProductActivityStatus.Error, marketplaceName: "Pazarama",
                        referenceId: record.BatchRequestId);
                }
                break;

            case StatusError:
                record.Status = MarketplaceProductStatus.Failed;
                record.StatusMessage = "Pazarama batch işlemi hata ile sonuçlandı.";
                logger.LogWarning("Pazarama batch {BatchId} errored — ProductId={ProductId}",
                    record.BatchRequestId, record.ProductId);

                await activityLogger.LogAsync(record.ProductId, ProductActivityType.BatchFailed,
                    "Pazarama batch hata ile sonuçlandı",
                    ProductActivityStatus.Error, marketplaceName: "Pazarama",
                    referenceId: record.BatchRequestId);
                break;

            default:
                logger.LogWarning("Pazarama batch {BatchId} has unknown status: {Status}",
                    record.BatchRequestId, batchStatus.Status);
                break;
        }
    }
}

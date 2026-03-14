using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Trendyol;
using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Periyodik olarak Pending + BatchRequestId olan ProductMarketplace kayıtlarını kontrol eder.
/// Trendyol batch API'sinden durum sorgular ve Published/Failed olarak günceller.
/// </summary>
public class TrendyolBatchStatusPollingService(
    IServiceScopeFactory scopeFactory,
    ILogger<TrendyolBatchStatusPollingService> logger) : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(60);

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
                logger.LogError(ex, "Batch status polling failed");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task PollPendingBatchesAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();
        var trendyolService = scope.ServiceProvider.GetRequiredService<ITrendyolProductService>();

        var pendingRecords = await dbContext.ProductMarketplaces
            .Where(pm => pm.Status == MarketplaceProductStatus.Pending &&
                         pm.BatchRequestId != null)
            .ToListAsync(ct);

        if (pendingRecords.Count == 0) return;

        logger.LogInformation("Polling {Count} pending batch requests", pendingRecords.Count);

        foreach (var record in pendingRecords)
        {
            try
            {
                var result = await trendyolService.CheckBatchStatusAsync(record.BatchRequestId!);
                if (!result.Success || result.Data is null) continue;

                if (result.Data.Status == TrendyolBatchStatus.IN_PROGRESS)
                    continue;

                // COMPLETED — item seviyesinde başarı/başarısızlık kontrol et
                if (result.Data.FailedItemCount > 0)
                {
                    record.Status = MarketplaceProductStatus.Failed;
                    var reasons = result.Data.Items?
                        .Where(i => i.FailureReasons is { Count: > 0 })
                        .SelectMany(i => i.FailureReasons!)
                        .ToList();
                    record.StatusMessage = reasons?.Count > 0
                        ? string.Join("; ", reasons)
                        : "Trendyol batch işlemi başarısız.";
                    logger.LogWarning("Batch {BatchId} has failures — ProductId={ProductId}: {Message}",
                        record.BatchRequestId, record.ProductId, record.StatusMessage);
                }
                else
                {
                    record.Status = MarketplaceProductStatus.Published;
                    record.LastSyncedAt = DateTimeOffset.UtcNow;
                    record.StatusMessage = null;
                    logger.LogInformation("Batch {BatchId} completed — ProductId={ProductId} published",
                        record.BatchRequestId, record.ProductId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to check batch status for {BatchId}", record.BatchRequestId);
            }
        }

        await dbContext.SaveChangesAsync(ct);
    }
}

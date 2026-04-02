using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Tenants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Trendyol;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Periyodik olarak Pending + BatchRequestId olan ProductMarketplace kayıtlarını kontrol eder.
/// Trendyol batch API'sinden durum sorgular ve Published/Failed olarak günceller.
/// Tum aktif tenant'lar icin calisir.
/// </summary>
public class TrendyolBatchStatusPollingService(
    IServiceScopeFactory scopeFactory,
    ITenantRegistry tenantRegistry,
    ILogger<TrendyolBatchStatusPollingService> logger)
    : TenantAwarePollingService(scopeFactory, tenantRegistry, logger)
{
    private static readonly TimeSpan BatchTimeout = TimeSpan.FromHours(24);

    protected override TimeSpan PollInterval => TimeSpan.FromSeconds(60);
    protected override string? RequiredFeature => "Permissions.Integrations.View";

    protected override async Task PollForTenantAsync(
        IServiceProvider services, int tenantId,
        DateTimeOffset lastPoll, CancellationToken ct)
    {
        var dbContextFactory = services.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var trendyolService = services.GetRequiredService<ITrendyolProductService>();
        var activityLogger = services.GetRequiredService<IProductActivityLogger>();

        var pendingRecords = await dbContext.ProductMarketplaces
            .Where(pm => pm.Status == MarketplaceProductStatus.Pending &&
                         pm.BatchRequestId != null)
            .ToListAsync(ct);

        if (pendingRecords.Count == 0) return;

        logger.LogInformation("Tenant {TenantId}: Polling {Count} pending batch requests",
            tenantId, pendingRecords.Count);

        foreach (var record in pendingRecords)
        {
            try
            {
                // Timeout kontrolü — 24 saat içinde tamamlanmadıysa Failed'a al
                if (DateTimeOffset.UtcNow - record.UpdatedAt > BatchTimeout)
                {
                    record.Status = MarketplaceProductStatus.Failed;
                    record.StatusMessage = "Batch işlemi 24 saat içinde tamamlanmadı — zaman aşımı.";
                    logger.LogWarning("Tenant {TenantId}: Batch {BatchId} timed out for ProductId={ProductId}",
                        tenantId, record.BatchRequestId, record.ProductId);
                    await activityLogger.LogAsync(record.ProductId, ProductActivityType.BatchFailed,
                        "Trendyol batch zaman aşımına uğradı (24 saat)", ProductActivityStatus.Error,
                        marketplaceName: "Trendyol", referenceId: record.BatchRequestId);
                    continue;
                }

                var result = await trendyolService.CheckBatchStatusAsync(record.BatchRequestId!);
                if (!result.Success || result.Data is null)
                {
                    logger.LogWarning("Tenant {TenantId}: Batch status check failed for {BatchId}, ProductId={ProductId}. Will retry next cycle.",
                        tenantId, record.BatchRequestId, record.ProductId);
                    continue;
                }

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
                    logger.LogWarning("Tenant {TenantId}: Batch {BatchId} has failures — ProductId={ProductId}: {Message}",
                        tenantId, record.BatchRequestId, record.ProductId, record.StatusMessage);

                    await activityLogger.LogAsync(record.ProductId, ProductActivityType.BatchFailed,
                        $"Trendyol batch başarısız: {record.StatusMessage}",
                        ProductActivityStatus.Error, marketplaceName: "Trendyol",
                        referenceId: record.BatchRequestId);
                }
                else
                {
                    record.Status = MarketplaceProductStatus.Published;
                    record.LastSyncedAt = DateTimeOffset.UtcNow;
                    record.StatusMessage = null;
                    logger.LogInformation("Tenant {TenantId}: Batch {BatchId} completed — ProductId={ProductId} published",
                        tenantId, record.BatchRequestId, record.ProductId);

                    await activityLogger.LogAsync(record.ProductId, ProductActivityType.BatchCompleted,
                        "Trendyol batch tamamlandı — ürün yayında",
                        ProductActivityStatus.Success, marketplaceName: "Trendyol",
                        referenceId: record.BatchRequestId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Tenant {TenantId}: Failed to check batch status for {BatchId}",
                    tenantId, record.BatchRequestId);
            }
        }

        await dbContext.SaveChangesAsync(ct);
    }
}

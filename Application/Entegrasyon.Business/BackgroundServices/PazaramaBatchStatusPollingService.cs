using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Pazarama;
using Entegrasyon.Business.Tenants;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Pazarama ürün batch durumu periyodik polling servisi.
/// Pending + BatchRequestId olan ProductMarketplace kayıtlarını izler.
/// Status: 1=InProgress (bekle), 2=Done (Published/Failed), 3=Error (Failed).
/// Tum aktif tenant'lar icin calisir.
/// </summary>
public class PazaramaBatchStatusPollingService(
    IServiceScopeFactory scopeFactory,
    ITenantRegistry tenantRegistry,
    ILogger<PazaramaBatchStatusPollingService> logger)
    : TenantAwarePollingService(scopeFactory, tenantRegistry, logger)
{
    private const int PazaramaMarketPlaceId = MarketPlaceConstants.PazaramaMarketPlaceId;
    private const int StatusInProgress = 1;
    private const int StatusDone = 2;
    private const int StatusError = 3;

    private static readonly TimeSpan BatchTimeout = TimeSpan.FromHours(24);

    protected override TimeSpan PollInterval => TimeSpan.FromSeconds(60);
    protected override string? RequiredFeature => "Permissions.Integrations.View";

    protected override async Task PollForTenantAsync(
        IServiceProvider services, int tenantId,
        DateTimeOffset lastPoll, CancellationToken ct)
    {
        var dbContextFactory = services.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var pazaramaService = services.GetRequiredService<IPazaramaProductService>();
        var activityLogger = services.GetRequiredService<IProductActivityLogger>();

        var pendingRecords = await dbContext.ProductMarketplaces
            .Where(pm => pm.MarketPlaceId == PazaramaMarketPlaceId &&
                         pm.Status == MarketplaceProductStatus.Pending &&
                         pm.BatchRequestId != null)
            .ToListAsync(ct);

        if (pendingRecords.Count == 0) return;

        logger.LogInformation("Tenant {TenantId}: Pazarama polling: {Count} pending batch requests",
            tenantId, pendingRecords.Count);

        foreach (var record in pendingRecords)
        {
            try
            {
                // Timeout kontrolü — 24 saat içinde tamamlanmadıysa Failed'a al
                if (DateTimeOffset.UtcNow - record.UpdatedAt > BatchTimeout)
                {
                    record.Status = MarketplaceProductStatus.Failed;
                    record.StatusMessage = "Pazarama batch işlemi 24 saat içinde tamamlanmadı — zaman aşımı.";
                    logger.LogWarning("Tenant {TenantId}: Pazarama batch {BatchId} timed out for ProductId={ProductId}",
                        tenantId, record.BatchRequestId, record.ProductId);
                    await activityLogger.LogAsync(record.ProductId, ProductActivityType.BatchFailed,
                        "Pazarama batch zaman aşımına uğradı (24 saat)", ProductActivityStatus.Error,
                        marketplaceName: "Pazarama", referenceId: record.BatchRequestId);
                    continue;
                }

                var result = await pazaramaService.CheckBatchStatusAsync(record.BatchRequestId!);
                if (!result.Success || result.Data is null)
                {
                    logger.LogWarning(
                        "Tenant {TenantId}: Pazarama batch status check failed for {BatchId}, ProductId={ProductId}. Will retry next cycle.",
                        tenantId, record.BatchRequestId, record.ProductId);
                    continue;
                }

                await ProcessBatchResultAsync(tenantId, record, result.Data, activityLogger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Tenant {TenantId}: Failed to check Pazarama batch status for {BatchId}",
                    tenantId, record.BatchRequestId);
            }
        }

        await dbContext.SaveChangesAsync(ct);
    }

    private async Task ProcessBatchResultAsync(
        int tenantId,
        ProductMarketplace record,
        PazaramaBatchStatusResponse batchStatus,
        IProductActivityLogger activityLogger)
    {
        switch (batchStatus.Status)
        {
            case StatusInProgress:
                // Henüz işleniyor — sonraki döngüde tekrar kontrol
                logger.LogDebug("Tenant {TenantId}: Pazarama batch {BatchId} still in progress, will retry",
                    tenantId, record.BatchRequestId);
                break;

            case StatusDone:
                if (batchStatus.FailedCount == 0)
                {
                    record.Status = MarketplaceProductStatus.Published;
                    record.LastSyncedAt = DateTimeOffset.UtcNow;
                    record.StatusMessage = null;
                    logger.LogInformation("Tenant {TenantId}: Pazarama batch {BatchId} completed — ProductId={ProductId} published",
                        tenantId, record.BatchRequestId, record.ProductId);

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
                    logger.LogWarning("Tenant {TenantId}: Pazarama batch {BatchId} has failures — ProductId={ProductId}: {Message}",
                        tenantId, record.BatchRequestId, record.ProductId, errorMessages);

                    await activityLogger.LogAsync(record.ProductId, ProductActivityType.BatchFailed,
                        $"Pazarama batch başarısız: {errorMessages}",
                        ProductActivityStatus.Error, marketplaceName: "Pazarama",
                        referenceId: record.BatchRequestId);
                }
                break;

            case StatusError:
                record.Status = MarketplaceProductStatus.Failed;
                record.StatusMessage = "Pazarama batch işlemi hata ile sonuçlandı.";
                logger.LogWarning("Tenant {TenantId}: Pazarama batch {BatchId} errored — ProductId={ProductId}",
                    tenantId, record.BatchRequestId, record.ProductId);

                await activityLogger.LogAsync(record.ProductId, ProductActivityType.BatchFailed,
                    "Pazarama batch hata ile sonuçlandı",
                    ProductActivityStatus.Error, marketplaceName: "Pazarama",
                    referenceId: record.BatchRequestId);
                break;

            default:
                logger.LogWarning("Tenant {TenantId}: Pazarama batch {BatchId} has unknown status: {Status}",
                    tenantId, record.BatchRequestId, batchStatus.Status);
                break;
        }
    }
}

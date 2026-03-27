using Entegrasyon.Business.Abstract;
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
/// Her 30 saniyede PttAVM tracking sonuclarini kontrol eder.
/// Pending/InProgress kayitlari sorgular, durumu gunceller.
/// Tum aktif tenant'lar icin calisir.
/// </summary>
public class PttavmProductTrackingPollingService(
    IServiceScopeFactory scopeFactory,
    ITenantRegistry tenantRegistry,
    ILogger<PttavmProductTrackingPollingService> logger)
    : TenantAwarePollingService(scopeFactory, tenantRegistry, logger)
{
    private const int PttavmMarketPlaceId = MarketPlaceConstants.PttavmMarketPlaceId;
    private static readonly TimeSpan TrackingTimeout = TimeSpan.FromHours(24);

    protected override TimeSpan PollInterval => TimeSpan.FromSeconds(30);
    protected override string? RequiredFeature => "Permissions.Integrations.View";

    protected override async Task PollForTenantAsync(
        IServiceProvider services, int tenantId,
        DateTimeOffset lastPoll, CancellationToken ct)
    {
        var dbContext = services.GetRequiredService<IntegrationDbContext>();
        var productService = services.GetRequiredService<IPttavmProductService>();
        var activityLogger = services.GetRequiredService<IProductActivityLogger>();

        var pendingRecords = await dbContext.ProductMarketplaces
            .Where(pm => pm.MarketPlaceId == PttavmMarketPlaceId &&
                         pm.Status == MarketplaceProductStatus.Pending &&
                         pm.BatchRequestId != null)
            .ToListAsync(ct);

        if (pendingRecords.Count == 0) return;

        logger.LogInformation("Tenant {TenantId}: PttAVM polling: {Count} pending tracking requests",
            tenantId, pendingRecords.Count);

        foreach (var record in pendingRecords)
        {
            try
            {
                // Timeout kontrolu
                if (DateTimeOffset.UtcNow - record.UpdatedAt > TrackingTimeout)
                {
                    record.Status = MarketplaceProductStatus.Failed;
                    record.StatusMessage = "PttAVM tracking işlemi 24 saat içinde tamamlanmadı — zaman aşımı.";
                    logger.LogWarning("Tenant {TenantId}: PttAVM tracking {TrackingId} timed out for ProductId={ProductId}",
                        tenantId, record.BatchRequestId, record.ProductId);
                    await activityLogger.LogAsync(record.ProductId, ProductActivityType.BatchFailed,
                        "PttAVM tracking zaman aşımına uğradı (24 saat)", ProductActivityStatus.Error,
                        marketplaceName: "PttAVM", referenceId: record.BatchRequestId);
                    continue;
                }

                var result = await productService.GetTrackingResultAsync(record.BatchRequestId!, ct);
                if (!result.Success || result.Data is null)
                {
                    logger.LogWarning(
                        "Tenant {TenantId}: PttAVM tracking check failed for {TrackingId}, ProductId={ProductId}. Will retry next cycle.",
                        tenantId, record.BatchRequestId, record.ProductId);
                    continue;
                }

                var trackingData = result.Data;
                var subResult = trackingData.ProductsSubTrackingResult;

                if (subResult is null)
                {
                    logger.LogDebug("Tenant {TenantId}: PttAVM tracking {TrackingId} has no sub-result yet, will retry",
                        tenantId, record.BatchRequestId);
                    continue;
                }

                // Hala isleniyor
                if (subResult.CountOfInProgressProducts > 0 || subResult.CountOfWaitingProducts > 0)
                {
                    logger.LogDebug("Tenant {TenantId}: PttAVM tracking {TrackingId} still in progress, will retry",
                        tenantId, record.BatchRequestId);
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
                    logger.LogWarning("Tenant {TenantId}: PttAVM tracking {TrackingId} has cancelled products: {Message}",
                        tenantId, record.BatchRequestId, record.StatusMessage);
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
                    logger.LogInformation("Tenant {TenantId}: PttAVM tracking {TrackingId} completed — ProductId={ProductId} published",
                        tenantId, record.BatchRequestId, record.ProductId);
                    await activityLogger.LogAsync(record.ProductId, ProductActivityType.BatchCompleted,
                        "PttAVM tracking tamamlandı — ürün yayında",
                        ProductActivityStatus.Success, marketplaceName: "PttAVM",
                        referenceId: record.BatchRequestId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Tenant {TenantId}: Failed to check PttAVM tracking for {TrackingId}",
                    tenantId, record.BatchRequestId);
            }
        }

        await dbContext.SaveChangesAsync(ct);
    }
}

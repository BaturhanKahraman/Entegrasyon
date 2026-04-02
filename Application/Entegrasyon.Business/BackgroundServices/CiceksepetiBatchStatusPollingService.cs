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
/// Periyodik olarak Çiçeksepeti'nde bekleyen batch işlemlerini kontrol eder.
/// Ürün batch'leri 24 saat içinde tamamlanmazsa Failed olarak işaretlenir.
/// Tum aktif tenant'lar icin calisir.
/// </summary>
public class CiceksepetiBatchStatusPollingService(
    IServiceScopeFactory scopeFactory,
    ITenantRegistry tenantRegistry,
    ILogger<CiceksepetiBatchStatusPollingService> logger)
    : TenantAwarePollingService(scopeFactory, tenantRegistry, logger)
{
    private const int CiceksepetiMarketPlaceId = MarketPlaceConstants.CiceksepetiMarketPlaceId;

    private static readonly TimeSpan ProductBatchTimeout = TimeSpan.FromHours(24);

    protected override TimeSpan PollInterval => TimeSpan.FromSeconds(30);
    protected override string? RequiredFeature => "Permissions.Integrations.View";

    protected override async Task PollForTenantAsync(
        IServiceProvider services, int tenantId,
        DateTimeOffset lastPoll, CancellationToken ct)
    {
        var dbContextFactory = services.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var productService = services.GetRequiredService<ICiceksepetiProductService>();
        var activityLogger = services.GetRequiredService<IProductActivityLogger>();

        var pendingRecords = await dbContext.ProductMarketplaces
            .Where(pm => pm.MarketPlaceId == CiceksepetiMarketPlaceId &&
                         pm.Status == MarketplaceProductStatus.Pending &&
                         pm.BatchRequestId != null)
            .ToListAsync(ct);

        if (pendingRecords.Count == 0) return;

        logger.LogInformation("Tenant {TenantId}: Çiçeksepeti polling: {Count} pending batch requests",
            tenantId, pendingRecords.Count);

        foreach (var record in pendingRecords)
        {
            try
            {
                // Timeout kontrolü — 24 saat içinde tamamlanmadıysa Failed'a al
                if (DateTimeOffset.UtcNow - record.UpdatedAt > ProductBatchTimeout)
                {
                    record.Status = MarketplaceProductStatus.Failed;
                    record.StatusMessage = "Çiçeksepeti batch işlemi 24 saat içinde tamamlanmadı — zaman aşımı.";
                    logger.LogWarning("Tenant {TenantId}: Çiçeksepeti batch {BatchId} timed out for ProductId={ProductId}",
                        tenantId, record.BatchRequestId, record.ProductId);
                    await activityLogger.LogAsync(record.ProductId, ProductActivityType.BatchFailed,
                        "Çiçeksepeti batch zaman aşımına uğradı (24 saat)", ProductActivityStatus.Error,
                        marketplaceName: "Çiçeksepeti", referenceId: record.BatchRequestId);
                    continue;
                }

                var result = await productService.CheckBatchStatusAsync(record.BatchRequestId!, ct);
                if (!result.Success || result.Data is null)
                {
                    logger.LogWarning(
                        "Tenant {TenantId}: Çiçeksepeti batch status check failed for {BatchId}, ProductId={ProductId}. Will retry next cycle.",
                        tenantId, record.BatchRequestId, record.ProductId);
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
                    logger.LogDebug("Tenant {TenantId}: Çiçeksepeti batch {BatchId} still in progress ({Count} items), will retry",
                        tenantId, record.BatchRequestId, inProgressItems.Count);
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
                    logger.LogWarning("Tenant {TenantId}: Çiçeksepeti batch {BatchId} has failures — ProductId={ProductId}: {Message}",
                        tenantId, record.BatchRequestId, record.ProductId, record.StatusMessage);
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
                    logger.LogInformation("Tenant {TenantId}: Çiçeksepeti batch {BatchId} completed — ProductId={ProductId} published",
                        tenantId, record.BatchRequestId, record.ProductId);
                    await activityLogger.LogAsync(record.ProductId, ProductActivityType.BatchCompleted,
                        "Çiçeksepeti batch tamamlandı — ürün yayında",
                        ProductActivityStatus.Success, marketplaceName: "Çiçeksepeti",
                        referenceId: record.BatchRequestId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Tenant {TenantId}: Failed to check Çiçeksepeti batch status for {BatchId}",
                    tenantId, record.BatchRequestId);
            }
        }

        await dbContext.SaveChangesAsync(ct);
    }
}

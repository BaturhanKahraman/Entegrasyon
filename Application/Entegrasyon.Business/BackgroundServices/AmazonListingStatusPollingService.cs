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
/// Amazon listing durumu periyodik polling servisi.
/// Pending ürünlerin Amazon'daki listing durumunu kontrol eder.
/// Tum aktif tenant'lar icin calisir.
/// </summary>
public class AmazonListingStatusPollingService(
    IServiceScopeFactory scopeFactory,
    ITenantRegistry tenantRegistry,
    ILogger<AmazonListingStatusPollingService> logger)
    : TenantAwarePollingService(scopeFactory, tenantRegistry, logger)
{
    private const int AmazonMpId = MarketPlaceConstants.AmazonMarketPlaceId;
    private static readonly TimeSpan TimeoutThreshold = TimeSpan.FromHours(24);

    protected override TimeSpan PollInterval => TimeSpan.FromMinutes(3);
    protected override string? RequiredFeature => "Permissions.Integrations.View";

    protected override async Task PollForTenantAsync(
        IServiceProvider services, int tenantId,
        DateTimeOffset lastPoll, CancellationToken ct)
    {
        var dbContextFactory = services.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        var productService = services.GetRequiredService<IAmazonProductService>();
        var activityLogger = services.GetRequiredService<IProductActivityLogger>();

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var pendingRecords = await dbContext.ProductMarketplaces
            .Where(pm => pm.MarketPlaceId == AmazonMpId &&
                         pm.Status == MarketplaceProductStatus.Pending &&
                         pm.ExternalProductId != null &&
                         (pm.BatchRequestId == null || !pm.BatchRequestId.StartsWith("feed-")))
            .ToListAsync(ct);

        if (!pendingRecords.Any()) return;

        logger.LogDebug("Tenant {TenantId}: Amazon listing polling: {Count} pending listings",
            tenantId, pendingRecords.Count);

        foreach (var record in pendingRecords)
        {
            // Timeout kontrolü
            if (DateTimeOffset.UtcNow - record.UpdatedAt > TimeoutThreshold)
            {
                record.Status = MarketplaceProductStatus.Failed;
                record.StatusMessage = "Amazon listing durumu 24 saat içinde çözümlenmedi (timeout).";
                record.UpdatedAt = DateTimeOffset.UtcNow;
                await activityLogger.LogAsync(record.ProductId, ProductActivityType.BatchFailed,
                    "Amazon listing timeout (24 saat)", ProductActivityStatus.Error,
                    marketplaceName: "Amazon", referenceId: record.ExternalProductId);
                continue;
            }

            var statusResult = await productService.CheckListingStatusAsync(record.ExternalProductId!, ct);
            if (!statusResult.Success || statusResult.Data == null) continue;

            var summaries = statusResult.Data.Summaries;
            if (summaries == null || !summaries.Any()) continue;

            var statuses = summaries.SelectMany(s => s.Status ?? []).ToList();

            if (statuses.Contains("BUYABLE"))
            {
                record.Status = MarketplaceProductStatus.Published;
                record.IsApproved = true;
                record.LastSyncedAt = DateTimeOffset.UtcNow;
                record.UpdatedAt = DateTimeOffset.UtcNow;

                var asin = summaries.FirstOrDefault()?.Asin;
                await activityLogger.LogAsync(record.ProductId, ProductActivityType.BatchCompleted,
                    $"Amazon listing aktif (ASIN: {asin})", ProductActivityStatus.Success,
                    marketplaceName: "Amazon", referenceId: record.ExternalProductId);
            }

            // Issues varsa logla
            var issues = statusResult.Data.Issues;
            if (issues?.Any() == true)
            {
                var issueMessages = string.Join(", ", issues.Select(i => i.Message));
                logger.LogWarning("Tenant {TenantId}: Amazon listing issues for {Sku}: {Issues}",
                    tenantId, record.ExternalProductId, issueMessages);
            }
        }

        await dbContext.SaveChangesAsync(ct);
    }
}

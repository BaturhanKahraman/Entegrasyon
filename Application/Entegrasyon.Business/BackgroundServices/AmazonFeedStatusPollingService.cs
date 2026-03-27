using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Tenants;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Amazon;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Amazon feed durumu periyodik polling servisi.
/// Submit edilen feed'lerin işlenme durumunu kontrol eder.
/// Tum aktif tenant'lar icin calisir.
/// </summary>
public class AmazonFeedStatusPollingService(
    IServiceScopeFactory scopeFactory,
    ITenantRegistry tenantRegistry,
    ILogger<AmazonFeedStatusPollingService> logger)
    : TenantAwarePollingService(scopeFactory, tenantRegistry, logger)
{
    private const int AmazonMpId = MarketPlaceConstants.AmazonMarketPlaceId;

    protected override TimeSpan PollInterval => TimeSpan.FromMinutes(5);
    protected override string? RequiredFeature => "Permissions.Integrations.View";

    protected override async Task PollForTenantAsync(
        IServiceProvider services, int tenantId,
        DateTimeOffset lastPoll, CancellationToken ct)
    {
        var dbContextFactory = services.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        var feedService = services.GetRequiredService<IAmazonFeedService>();
        var activityLogger = services.GetRequiredService<IProductActivityLogger>();

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        // BatchRequestId'de feed ID saklanan pending kayıtlar
        var pendingRecords = await dbContext.ProductMarketplaces
            .Where(pm => pm.MarketPlaceId == AmazonMpId &&
                         pm.Status == MarketplaceProductStatus.Pending &&
                         pm.BatchRequestId != null &&
                         pm.BatchRequestId.StartsWith("feed-"))
            .ToListAsync(ct);

        if (!pendingRecords.Any()) return;

        logger.LogDebug("Tenant {TenantId}: Amazon feed polling: {Count} pending feeds",
            tenantId, pendingRecords.Count);

        foreach (var record in pendingRecords)
        {
            var statusResult = await feedService.GetFeedStatusAsync(record.BatchRequestId!, ct);
            if (!statusResult.Success || statusResult.Data == null) continue;

            var status = statusResult.Data.ProcessingStatus;

            switch (status)
            {
                case AmazonFeedStatus.Done:
                    record.Status = MarketplaceProductStatus.Published;
                    record.LastSyncedAt = DateTimeOffset.UtcNow;
                    record.UpdatedAt = DateTimeOffset.UtcNow;
                    await activityLogger.LogAsync(record.ProductId, ProductActivityType.BatchCompleted,
                        "Amazon feed işleme tamamlandı", ProductActivityStatus.Success,
                        marketplaceName: "Amazon", referenceId: record.BatchRequestId);
                    break;

                case AmazonFeedStatus.Fatal:
                case AmazonFeedStatus.Cancelled:
                    record.Status = MarketplaceProductStatus.Failed;
                    record.StatusMessage = $"Feed {status}";
                    record.UpdatedAt = DateTimeOffset.UtcNow;
                    await activityLogger.LogAsync(record.ProductId, ProductActivityType.BatchFailed,
                        $"Amazon feed başarısız: {status}", ProductActivityStatus.Error,
                        marketplaceName: "Amazon", referenceId: record.BatchRequestId);
                    break;

                    // IN_QUEUE, IN_PROGRESS → sonraki döngüde tekrar kontrol
            }
        }

        await dbContext.SaveChangesAsync(ct);
    }
}

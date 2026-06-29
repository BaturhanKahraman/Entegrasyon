using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Tenants;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.N11;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// N11 REST task-based işlemlerin sonucunu periyodik olarak takip eder.
/// ProductMarketplace kayıtlarında BatchRequestId olan ve Status=Pending olan
/// ürünlerin task durumunu /ms/product/task-details/page-query ile sorgular.
/// PROCESSED → SUCCESS ise Published, FAIL ise Failed olarak günceller.
/// </summary>
public class N11TaskPollingService(
    IServiceScopeFactory scopeFactory,
    ITenantRegistry tenantRegistry,
    ILogger<N11TaskPollingService> logger)
    : TenantAwarePollingService(scopeFactory, tenantRegistry, logger)
{
    private const int N11MarketPlaceId = MarketPlaceConstants.N11MarketPlaceId;
    private static readonly TimeSpan TaskTimeout = TimeSpan.FromHours(24);

    protected override TimeSpan PollInterval => TimeSpan.FromMinutes(2);
    protected override string? RequiredFeature => "Permissions.Integrations.View";

    protected override async Task PollForTenantAsync(
        IServiceProvider services, int tenantId,
        DateTimeOffset lastPoll, CancellationToken ct)
    {
        var dbContext = services.GetRequiredService<IntegrationDbContext>();
        var restClient = services.GetRequiredService<IN11RestClient>();
        var activityLogger = services.GetRequiredService<IProductActivityLogger>();

        var pendingRecords = await dbContext.ProductMarketplaces
            .AsTracking() // mutasyon: global no-tracking → SaveChanges sessiz no-op olmasın
            .Where(pm => pm.MarketPlaceId == N11MarketPlaceId &&
                         pm.Status == MarketplaceProductStatus.Pending &&
                         pm.BatchRequestId != null)
            .ToListAsync(ct);

        if (pendingRecords.Count == 0) return;

        logger.LogInformation("Tenant {TenantId}: N11 task polling: {Count} bekleyen task",
            tenantId, pendingRecords.Count);

        foreach (var record in pendingRecords)
        {
            try
            {
                // Timeout kontrolü — 24 saat geçtiyse Failed'a al
                if (DateTimeOffset.UtcNow - record.UpdatedAt > TaskTimeout)
                {
                    record.Status = MarketplaceProductStatus.Failed;
                    record.StatusMessage = "N11 task 24 saat içinde tamamlanmadı — zaman aşımı.";
                    logger.LogWarning("Tenant {TenantId}: N11 task {TaskId} timed out — ProductId={ProductId}",
                        tenantId, record.BatchRequestId, record.ProductId);
                    await activityLogger.LogAsync(record.ProductId, ProductActivityType.BatchFailed,
                        "N11 task zaman aşımına uğradı (24 saat)", ProductActivityStatus.Error,
                        marketplaceName: "N11", referenceId: record.BatchRequestId);
                    continue;
                }

                if (!long.TryParse(record.BatchRequestId, out var taskId))
                {
                    logger.LogWarning("Tenant {TenantId}: N11 geçersiz BatchRequestId={BatchRequestId} — ProductId={ProductId}",
                        tenantId, record.BatchRequestId, record.ProductId);
                    continue;
                }

                var detailRequest = new N11TaskDetailRequest(
                    TaskId: taskId,
                    Pageable: new N11Pageable(Page: 0, Size: 1000));

                var detail = await restClient.PostAsync<N11TaskDetailRequest, N11TaskDetailResponse>(
                    "ms/product/task-details/page-query", detailRequest, ct);

                if (detail is null)
                {
                    logger.LogWarning("Tenant {TenantId}: N11 task detail yanıt boş — TaskId={TaskId}", tenantId, taskId);
                    continue;
                }

                await ProcessTaskDetailAsync(tenantId, record, detail, activityLogger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Tenant {TenantId}: N11 task polling hatası — BatchRequestId={BatchRequestId}",
                    tenantId, record.BatchRequestId);
            }
        }

        await dbContext.SaveChangesAsync(ct);
    }

    private async Task ProcessTaskDetailAsync(
        int tenantId,
        ProductMarketplace record,
        N11TaskDetailResponse detail,
        IProductActivityLogger activityLogger)
    {
        switch (detail.Status)
        {
            case "IN_QUEUE":
                // Henüz işleniyor — sonraki döngüde tekrar kontrol
                logger.LogDebug("Tenant {TenantId}: N11 task {TaskId} hâlâ kuyrukta, bekleniyor",
                    tenantId, detail.TaskId);
                break;

            case "PROCESSED":
                var content = detail.Skus?.Content ?? [];
                var failedItems = content.Where(c => c.Status != "SUCCESS").ToList();

                if (failedItems.Count == 0)
                {
                    record.Status = MarketplaceProductStatus.Published;
                    record.LastSyncedAt = DateTimeOffset.UtcNow;
                    record.StatusMessage = null;

                    logger.LogInformation("Tenant {TenantId}: N11 task {TaskId} tamamlandı — ProductId={ProductId} yayında",
                        tenantId, detail.TaskId, record.ProductId);

                    await activityLogger.LogAsync(record.ProductId, ProductActivityType.BatchCompleted,
                        $"N11 task tamamlandı — ürün yayında (TaskId: {detail.TaskId})",
                        ProductActivityStatus.Success, marketplaceName: "N11",
                        referenceId: record.BatchRequestId);
                }
                else
                {
                    var errorMessages = string.Join("; ", failedItems
                        .SelectMany(f => f.Reasons ?? [f.Status]));

                    record.Status = MarketplaceProductStatus.Failed;
                    record.StatusMessage = errorMessages;

                    logger.LogWarning("Tenant {TenantId}: N11 task {TaskId} başarısız — ProductId={ProductId}: {Message}",
                        tenantId, detail.TaskId, record.ProductId, errorMessages);

                    await activityLogger.LogAsync(record.ProductId, ProductActivityType.BatchFailed,
                        $"N11 task başarısız: {errorMessages}",
                        ProductActivityStatus.Error, marketplaceName: "N11",
                        referenceId: record.BatchRequestId);
                }
                break;

            case "REJECT":
                record.Status = MarketplaceProductStatus.Failed;
                record.StatusMessage = "N11 task reddedildi.";

                logger.LogWarning("Tenant {TenantId}: N11 task {TaskId} reddedildi — ProductId={ProductId}",
                    tenantId, detail.TaskId, record.ProductId);

                await activityLogger.LogAsync(record.ProductId, ProductActivityType.BatchFailed,
                    "N11 task reddedildi",
                    ProductActivityStatus.Error, marketplaceName: "N11",
                    referenceId: record.BatchRequestId);
                break;

            default:
                logger.LogWarning("Tenant {TenantId}: N11 task {TaskId} bilinmeyen durum: {Status}",
                    tenantId, detail.TaskId, detail.Status);
                break;
        }
    }
}

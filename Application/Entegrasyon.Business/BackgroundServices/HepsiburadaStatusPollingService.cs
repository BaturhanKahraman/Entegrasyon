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
/// Hepsiburada ürün durumu periyodik polling servisi.
/// Pending + trackingId olan ProductMarketplace kayıtlarını izler.
/// PRE_MATCHED → otomatik onay, CREATED → Published, REJECTED/BLOCKED → Failed.
/// Tum aktif tenant'lar icin calisir.
/// </summary>
public class HepsiburadaStatusPollingService(
    IServiceScopeFactory scopeFactory,
    ITenantRegistry tenantRegistry,
    ILogger<HepsiburadaStatusPollingService> logger)
    : TenantAwarePollingService(scopeFactory, tenantRegistry, logger)
{
    private const int HbMarketPlaceId = MarketPlaceConstants.HepsiburadaMarketPlaceId;
    private static readonly TimeSpan TimeoutThreshold = TimeSpan.FromHours(24);

    protected override TimeSpan PollInterval => TimeSpan.FromSeconds(60);
    protected override string? RequiredFeature => "Permissions.Integrations.View";

    protected override async Task PollForTenantAsync(
        IServiceProvider services, int tenantId,
        DateTimeOffset lastPoll, CancellationToken ct)
    {
        var dbContextFactory = services.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        var productService = services.GetRequiredService<IHepsiburadaProductService>();
        var activityLogger = services.GetRequiredService<IProductActivityLogger>();

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var pendingRecords = await dbContext.ProductMarketplaces
            .Where(pm => pm.MarketPlaceId == HbMarketPlaceId &&
                         pm.Status == MarketplaceProductStatus.Pending &&
                         pm.BatchRequestId != null)
            .ToListAsync(ct);

        if (!pendingRecords.Any()) return;

        logger.LogDebug("Tenant {TenantId}: HB polling: {Count} pending records",
            tenantId, pendingRecords.Count);

        foreach (var record in pendingRecords)
        {
            // Timeout kontrolü
            if (DateTimeOffset.UtcNow - record.UpdatedAt > TimeoutThreshold)
            {
                record.Status = MarketplaceProductStatus.Failed;
                record.StatusMessage = "Hepsiburada ürün durumu 24 saat içinde çözümlenmedi (timeout).";
                record.UpdatedAt = DateTimeOffset.UtcNow;

                await activityLogger.LogAsync(record.ProductId, ProductActivityType.BatchFailed,
                    "Hepsiburada timeout (24 saat)", ProductActivityStatus.Error,
                    marketplaceName: "Hepsiburada", referenceId: record.BatchRequestId);
                continue;
            }

            var statusResult = await productService.CheckProductStatusAsync(record.BatchRequestId!);
            if (!statusResult.Success || statusResult.Data == null || !statusResult.Data.Any())
                continue;

            foreach (var item in statusResult.Data)
            {
                await ProcessStatusItemAsync(tenantId, dbContext, record, item, productService, activityLogger, ct);
            }
        }

        await dbContext.SaveChangesAsync(ct);
    }

    private async Task ProcessStatusItemAsync(
        int tenantId,
        IntegrationDbContext dbContext,
        ProductMarketplace record,
        Entity.Dtos.Hepsiburada.HepsiburadaProductStatusItem item,
        IHepsiburadaProductService productService,
        IProductActivityLogger activityLogger,
        CancellationToken ct)
    {
        var importStatus = item.ImportStatus?.ToUpperInvariant();
        var productStatus = item.ProductStatus?.ToUpperInvariant();

        switch (importStatus)
        {
            case "PROCESSING":
                // Henüz işleniyor, sonraki döngüde tekrar kontrol
                break;

            case "SUCCESS":
                await HandleSuccessStatusAsync(tenantId, record, item, productStatus, productService, activityLogger);
                break;

            case "FAILED":
                var importErrors = item.ImportMessages != null
                    ? string.Join(", ", item.ImportMessages.Select(m => m.Message))
                    : "Bilinmeyen import hatası";
                record.Status = MarketplaceProductStatus.Failed;
                record.StatusMessage = importErrors;
                record.UpdatedAt = DateTimeOffset.UtcNow;

                await activityLogger.LogAsync(record.ProductId, ProductActivityType.BatchFailed,
                    $"Hepsiburada import başarısız: {importErrors}",
                    ProductActivityStatus.Error, null, "Hepsiburada", record.BatchRequestId);
                break;

            default:
                logger.LogWarning("Tenant {TenantId}: HB unknown importStatus: {Status} for product {ProductId}",
                    tenantId, importStatus, record.ProductId);
                break;
        }
    }

    private async Task HandleSuccessStatusAsync(
        int tenantId,
        ProductMarketplace record,
        Entity.Dtos.Hepsiburada.HepsiburadaProductStatusItem item,
        string? productStatus,
        IHepsiburadaProductService productService,
        IProductActivityLogger activityLogger)
    {
        switch (productStatus)
        {
            case "PRE_MATCHED":
                // Otomatik onay
                logger.LogInformation("Tenant {TenantId}: HB PRE_MATCHED detected, auto-approving: {MerchantSku}",
                    tenantId, item.MerchantSku);
                if (item.MerchantSku != null)
                {
                    var approveResult = await productService.ApprovePreMatchAsync(item.MerchantSku);
                    await activityLogger.LogAsync(record.ProductId, ProductActivityType.Approved,
                        approveResult.Success
                            ? "Hepsiburada PRE_MATCHED otomatik onaylandı"
                            : $"Hepsiburada PRE_MATCHED onay hatası: {approveResult.Message}",
                        approveResult.Success ? ProductActivityStatus.Success : ProductActivityStatus.Error,
                        marketplaceName: "Hepsiburada", referenceId: record.BatchRequestId);
                }
                // Onaydan sonra sonraki döngüde tekrar kontrol edilecek
                break;

            case "MATCHED":
            case "CREATED":
                record.Status = MarketplaceProductStatus.Published;
                record.ExternalProductId = item.HbSku;
                record.IsApproved = true;
                record.LastSyncedAt = DateTimeOffset.UtcNow;
                record.UpdatedAt = DateTimeOffset.UtcNow;

                await activityLogger.LogAsync(record.ProductId, ProductActivityType.BatchCompleted,
                    $"Hepsiburada ürün başarıyla oluşturuldu (HB SKU: {item.HbSku})",
                    ProductActivityStatus.Success, null, "Hepsiburada", record.BatchRequestId);
                break;

            case "REJECTED":
            case "BLOCKED":
                var reasons = item.ValidationResults != null
                    ? string.Join(", ", item.ValidationResults.Select(v => v.Message))
                    : "Bilinmeyen red nedeni";
                record.Status = MarketplaceProductStatus.Failed;
                record.StatusMessage = $"{productStatus}: {reasons}";
                record.UpdatedAt = DateTimeOffset.UtcNow;

                await activityLogger.LogAsync(record.ProductId, ProductActivityType.Rejected,
                    $"Hepsiburada ürün reddedildi: {reasons}",
                    ProductActivityStatus.Error, null, "Hepsiburada", record.BatchRequestId);
                break;

            case "MISSING_INFO":
                var missingInfo = item.ValidationResults != null
                    ? string.Join(", ", item.ValidationResults.Select(v => $"{v.FieldName}: {v.Message}"))
                    : "Eksik bilgi";
                record.Status = MarketplaceProductStatus.Failed;
                record.StatusMessage = $"MISSING_INFO: {missingInfo}";
                record.UpdatedAt = DateTimeOffset.UtcNow;

                await activityLogger.LogAsync(record.ProductId, ProductActivityType.BatchFailed,
                    $"Hepsiburada eksik bilgi: {missingInfo}",
                    ProductActivityStatus.Error, null, "Hepsiburada", record.BatchRequestId);
                break;

            case "WAITING":
            case "IN_EXTERNAL_PROGRESS":
                // Beklemeye devam
                break;

            default:
                logger.LogWarning("Tenant {TenantId}: HB unknown productStatus: {Status}",
                    tenantId, productStatus);
                break;
        }
    }
}

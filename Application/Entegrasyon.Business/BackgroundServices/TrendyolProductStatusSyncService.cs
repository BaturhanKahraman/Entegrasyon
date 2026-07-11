using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Marketplace;
using Entegrasyon.Business.FeatureFlags;
using Entegrasyon.Business.Tenants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Trendyol;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Periyodik olarak Trendyol'dan ürün onay/red/arşiv durumlarını çeker ve
/// ProductMarketplace.IsApproved / IsArchived / ContentId alanlarını günceller.
/// Her 5 dakikada bir çalışır. Tum aktif tenant'lar icin calisir.
/// </summary>
public class TrendyolProductStatusSyncService(
    IServiceScopeFactory scopeFactory,
    ITenantRegistry tenantRegistry,
    ILogger<TrendyolProductStatusSyncService> logger,
    IOptions<NotificationFeatureFlags> notificationFlags)
    : TenantAwarePollingService(scopeFactory, tenantRegistry, logger)
{
    protected override TimeSpan PollInterval => TimeSpan.FromMinutes(5);
    protected override string? RequiredFeature => "Permissions.Integrations.View";

    protected override async Task PollForTenantAsync(
        IServiceProvider services, int tenantId,
        DateTimeOffset lastPoll, CancellationToken ct)
    {
        var dbContext = services.GetRequiredService<IntegrationDbContext>();
        var apiClient = services.GetRequiredService<ITrendyolApiClient>();
        var activityLogger = services.GetRequiredService<IProductActivityLogger>();

        // Trendyol'da published durumunda olan ürünleri al
        var trackedProducts = await dbContext.ProductMarketplaces
            .AsTracking() // mutasyon: global no-tracking → SaveChanges sessiz no-op olmasın
            .Include(pm => pm.Product).ThenInclude(p => p.ProductVariants)
            .Where(pm => pm.MarketPlaceId == TrendyolMarketPlaceId
                && (pm.Status == MarketplaceProductStatus.Published
                    || pm.Status == MarketplaceProductStatus.Rejected))
            .ToListAsync(ct);

        if (trackedProducts.Count == 0) return;

        var marketplace = await dbContext.MarketPlaces.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == TrendyolMarketPlaceId, ct);

        if (marketplace?.SellerId is null)
        {
            logger.LogWarning("Tenant {TenantId}: Trendyol SellerId not configured, skipping status sync",
                tenantId);
            return;
        }

        // Her ürünün ilk varyant barkodunu kullanarak durum sorgula
        foreach (var pm in trackedProducts)
        {
            try
            {
                var barcode = pm.Product.ProductVariants
                    .Select(v => v.Barcode)
                    .FirstOrDefault(b => !string.IsNullOrEmpty(b));

                if (string.IsNullOrEmpty(barcode)) continue;

                // İki adımlı durum sorgusu: önce onaylı liste, yoksa onaysız/red liste.
                var status = await QueryBarcodeStatusAsync(apiClient, marketplace.SellerId, barcode, ct);
                if (status is null)
                {
                    // Barkod her iki listede de yok → mevcut DB durumunu DEĞİŞTİRME, uyar.
                    logger.LogWarning(
                        "Tenant {TenantId}: Product {ProductId} barcode {Barcode} not found in approved/unapproved lists; status unchanged",
                        tenantId, pm.ProductId, barcode);
                    continue;
                }

                var s = status.Value;
                var isApproved = s.State == ApprovalState.Approved;
                var isRejected = s.State == ApprovalState.Rejected;

                var changed = false;
                if (pm.IsApproved != isApproved) { pm.IsApproved = isApproved; changed = true; }
                if (pm.IsArchived != s.Archived) { pm.IsArchived = s.Archived; changed = true; }
                if (s.ContentId.HasValue && pm.ContentId != s.ContentId)
                {
                    pm.ContentId = s.ContentId;
                    changed = true;
                }

                if (isRejected && pm.Status != MarketplaceProductStatus.Rejected)
                {
                    pm.Status = MarketplaceProductStatus.Rejected;
                    pm.StatusMessage = s.StatusMessage;
                    changed = true;

                    await activityLogger.LogAsync(pm.ProductId, ProductActivityType.Rejected,
                        $"Trendyol tarafından reddedildi: {pm.StatusMessage}",
                        ProductActivityStatus.Error, marketplaceName: "Trendyol");

                    if (notificationFlags.Value.PublishEnabled)
                    {
                        dbContext.AddDomainEvent(new MarketplaceProductRejectedEvent(
                            marketPlaceId: TrendyolMarketPlaceId,
                            productId: pm.ProductId,
                            rejectionReason: pm.StatusMessage ?? string.Empty));
                    }
                }

                // Rejected → Published recovery: Trendyol'da tekrar onaylanmışsa
                if (isApproved && pm.Status == MarketplaceProductStatus.Rejected)
                {
                    pm.Status = MarketplaceProductStatus.Published;
                    pm.LastSyncedAt = DateTimeOffset.UtcNow;
                    pm.StatusMessage = null;
                    changed = true;

                    await activityLogger.LogAsync(pm.ProductId, ProductActivityType.Approved,
                        "Trendyol tarafından yeniden onaylandı (önceki red kaldırıldı)",
                        ProductActivityStatus.Success, marketplaceName: "Trendyol",
                        referenceId: s.ContentId?.ToString());

                    if (notificationFlags.Value.PublishEnabled)
                    {
                        dbContext.AddDomainEvent(new MarketplaceProductApprovedEvent(
                            marketPlaceId: TrendyolMarketPlaceId,
                            productId: pm.ProductId,
                            marketplaceProductCode: s.ContentId?.ToString() ?? string.Empty));
                    }
                }

                if (isApproved && pm.IsApproved != true)
                {
                    await activityLogger.LogAsync(pm.ProductId, ProductActivityType.Approved,
                        "Trendyol tarafından onaylandı",
                        ProductActivityStatus.Success, marketplaceName: "Trendyol",
                        referenceId: s.ContentId?.ToString());

                    if (notificationFlags.Value.PublishEnabled)
                    {
                        dbContext.AddDomainEvent(new MarketplaceProductApprovedEvent(
                            marketPlaceId: TrendyolMarketPlaceId,
                            productId: pm.ProductId,
                            marketplaceProductCode: s.ContentId?.ToString() ?? string.Empty));
                    }
                }

                if (s.Archived && pm.IsArchived != true)
                {
                    await activityLogger.LogAsync(pm.ProductId, ProductActivityType.Archived,
                        "Trendyol'da arşivlendi",
                        ProductActivityStatus.Warning, marketplaceName: "Trendyol");
                }

                if (changed)
                {
                    logger.LogInformation(
                        "Tenant {TenantId}: Product {ProductId} status updated: Approved={Approved}, Archived={Archived}, ContentId={ContentId}",
                        tenantId, pm.ProductId, isApproved, s.Archived, s.ContentId);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Tenant {TenantId}: Failed to sync status for product {ProductId}",
                    tenantId, pm.ProductId);
            }
        }

        await dbContext.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Bir barkodun Trendyol durumunu iki adımlı sorgular:
    /// (1) Onaylı ürün listesi (products/approved) — barkod varsa ONAYLI.
    /// (2) Aksi halde onaysız/red listesi (products/unapproved) — rejectReasonDetails doluysa
    ///     REDDEDİLMİŞ, boşsa inceleniyor (pendingApproval).
    /// Her iki listede de bulunamazsa null döner; çağıran DB durumunu değiştirmez.
    /// </summary>
    private static async Task<BarcodeStatus?> QueryBarcodeStatusAsync(
        ITrendyolApiClient apiClient, string sellerId, string barcode, CancellationToken ct)
    {
        // Adım 1: Onaylı ürün listesi
        var approvedUrl = $"integration/product/sellers/{sellerId}/products/approved?barcode={barcode}";
        var approvedResponse = await apiClient.GetAsync(approvedUrl);
        if (approvedResponse.IsSuccessStatusCode)
        {
            var approved = await approvedResponse.Content
                .ReadFromJsonAsync<TrendyolApprovedProductsResponse>(cancellationToken: ct);

            // ?barcode= zaten server-side filtreli — adım 1 gibi ilk kaydı güven.
            var product = approved?.Content?.FirstOrDefault();
            if (product is not null)
            {
                var variant = product.Variants?
                    .FirstOrDefault(v => string.Equals(v.Barcode, barcode, StringComparison.OrdinalIgnoreCase));

                return new BarcodeStatus(
                    ApprovalState.Approved,
                    Archived: variant?.Archived ?? false,
                    ContentId: product.ContentId,
                    StatusMessage: null);
            }
        }

        // Adım 2: Onaysız/red ürün listesi
        var unapprovedUrl = $"integration/product/sellers/{sellerId}/products/unapproved?barcode={barcode}";
        var unapprovedResponse = await apiClient.GetAsync(unapprovedUrl);
        if (!unapprovedResponse.IsSuccessStatusCode)
            return null;

        var unapproved = await unapprovedResponse.Content
            .ReadFromJsonAsync<TrendyolUnapprovedProductsResponse>(cancellationToken: ct);

        var unapprovedProduct = unapproved?.Content?.FirstOrDefault();
        if (unapprovedProduct is null)
            return null;

        var rejectDetails = unapprovedProduct.RejectReasonDetails;
        if (rejectDetails is { Count: > 0 })
        {
            var detail = rejectDetails[0];
            var statusMessage = !string.IsNullOrWhiteSpace(detail.DetailedReason)
                ? detail.DetailedReason
                : detail.Reason;

            return new BarcodeStatus(
                ApprovalState.Rejected, Archived: false,
                ContentId: null, StatusMessage: statusMessage);
        }

        // İnceleme bekliyor (pendingApproval): ne onaylı ne reddedilmiş.
        return new BarcodeStatus(
            ApprovalState.Pending, Archived: false,
            ContentId: null, StatusMessage: null);
    }

    /// <summary>Trendyol onay durumu — Approved/Rejected/Pending birbirini dışlar.</summary>
    private enum ApprovalState { Approved, Rejected, Pending }

    /// <summary>Bir barkod için çözümlenmiş Trendyol durum özeti.</summary>
    private readonly record struct BarcodeStatus(
        ApprovalState State, bool Archived, long? ContentId, string? StatusMessage);
}

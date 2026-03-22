using System.Net.Http.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Periyodik olarak Trendyol'dan ürün onay/red/arşiv durumlarını çeker ve
/// ProductMarketplace.IsApproved / IsArchived / ContentId alanlarını günceller.
/// Her 5 dakikada bir çalışır.
/// </summary>
public class TrendyolProductStatusSyncService(
    IServiceScopeFactory scopeFactory,
    ILogger<TrendyolProductStatusSyncService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // İlk çalışmadan önce kısa bir gecikme
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncProductStatusesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during Trendyol product status sync");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task SyncProductStatusesAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();
        var apiClient = scope.ServiceProvider.GetRequiredService<ITrendyolApiClient>();
        var activityLogger = scope.ServiceProvider.GetRequiredService<IProductActivityLogger>();

        // Trendyol'da published durumunda olan ürünleri al
        var trackedProducts = await dbContext.ProductMarketplaces
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
            logger.LogWarning("Trendyol SellerId not configured, skipping status sync");
            return;
        }

        // Her ürünün ilk varyant barkodunu kullanarak durum sorgula
        foreach (var pm in trackedProducts)
        {
            try
            {
                var barcodes = pm.Product.ProductVariants
                    .Select(v => v.Barcode)
                    .Where(b => !string.IsNullOrEmpty(b))
                    .ToList();

                if (barcodes.Count == 0) continue;

                // İlk barkod ile sorgula
                var barcode = barcodes.First();
                var url = $"integration/product/sellers/{marketplace.SellerId}/products?barcode={barcode}";
                var response = await apiClient.GetAsync(url);

                if (!response.IsSuccessStatusCode) continue;

                var statusResponse = await response.Content
                    .ReadFromJsonAsync<Entity.Dtos.Trendyol.TrendyolProductStatusResponse>(cancellationToken: ct);

                var content = statusResponse?.Content?.FirstOrDefault();
                if (content is null) continue;

                var changed = false;
                if (pm.IsApproved != content.Approved) { pm.IsApproved = content.Approved; changed = true; }
                if (pm.IsArchived != content.Archived) { pm.IsArchived = content.Archived; changed = true; }
                if (content.ContentId.HasValue && pm.ContentId != content.ContentId)
                {
                    pm.ContentId = content.ContentId;
                    changed = true;
                }

                if (content.Rejected && pm.Status != MarketplaceProductStatus.Rejected)
                {
                    pm.Status = MarketplaceProductStatus.Rejected;
                    pm.StatusMessage = content.RejectReasonDetails?.FirstOrDefault()?.DetailedReason;
                    changed = true;

                    await activityLogger.LogAsync(pm.ProductId, ProductActivityType.Rejected,
                        $"Trendyol tarafından reddedildi: {pm.StatusMessage}",
                        ProductActivityStatus.Error, marketplaceName: "Trendyol");
                }

                // Rejected → Published recovery: Trendyol'da tekrar onaylanmışsa
                if (!content.Rejected && content.Approved
                    && pm.Status == MarketplaceProductStatus.Rejected)
                {
                    pm.Status = MarketplaceProductStatus.Published;
                    pm.LastSyncedAt = DateTimeOffset.UtcNow;
                    pm.StatusMessage = null;
                    changed = true;

                    await activityLogger.LogAsync(pm.ProductId, ProductActivityType.Approved,
                        "Trendyol tarafından yeniden onaylandı (önceki red kaldırıldı)",
                        ProductActivityStatus.Success, marketplaceName: "Trendyol",
                        referenceId: content.ContentId?.ToString());
                }

                if (content.Approved && pm.IsApproved != true)
                {
                    await activityLogger.LogAsync(pm.ProductId, ProductActivityType.Approved,
                        "Trendyol tarafından onaylandı",
                        ProductActivityStatus.Success, marketplaceName: "Trendyol",
                        referenceId: content.ContentId?.ToString());
                }

                if (content.Archived && pm.IsArchived != true)
                {
                    await activityLogger.LogAsync(pm.ProductId, ProductActivityType.Archived,
                        "Trendyol'da arşivlendi",
                        ProductActivityStatus.Warning, marketplaceName: "Trendyol");
                }

                if (changed)
                {
                    logger.LogInformation(
                        "Product {ProductId} status updated: Approved={Approved}, Archived={Archived}, ContentId={ContentId}",
                        pm.ProductId, content.Approved, content.Archived, content.ContentId);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to sync status for product {ProductId}", pm.ProductId);
            }
        }

        await dbContext.SaveChangesAsync(ct);
    }
}

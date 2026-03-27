using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Tenants;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Amazon stok ve fiyat periyodik senkronizasyon servisi.
/// Published ürünlerin stok/fiyat bilgilerini Feeds API üzerinden gönderir.
/// </summary>
public class AmazonStockPriceSyncService(
    IServiceScopeFactory scopeFactory,
    ITenantRegistry tenantRegistry,
    ILogger<AmazonStockPriceSyncService> logger)
    : TenantAwarePollingService(scopeFactory, tenantRegistry, logger)
{
    private const int AmazonMpId = MarketPlaceConstants.AmazonMarketPlaceId;

    protected override TimeSpan PollInterval => TimeSpan.FromMinutes(15);

    protected override async Task PollForTenantAsync(
        IServiceProvider services, int tenantId,
        DateTimeOffset lastPoll, CancellationToken ct)
    {
        var dbContextFactory = services.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        var feedService = services.GetRequiredService<IAmazonFeedService>();

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var publishedProducts = await dbContext.ProductMarketplaces
            .Where(pm => pm.MarketPlaceId == AmazonMpId &&
                         pm.Status == MarketplaceProductStatus.Published &&
                         pm.ExternalProductId != null)
            .Include(pm => pm.Product)
                .ThenInclude(p => p.ProductVariants)
                    .ThenInclude(v => v.BranchOfficeStocks)
            .ToListAsync(ct);

        if (!publishedProducts.Any())
        {
            logger.LogDebug("Amazon stock/price sync: yayında ürün yok, tenant {TenantId}", tenantId);
            return;
        }

        // TODO: JSON_LISTINGS_FEED formatında stok/fiyat feed oluştur ve gönder
        // var feedContent = BuildStockPriceFeed(publishedProducts);
        // await feedService.SubmitFeedAsync("JSON_LISTINGS_FEED", "application/json", feedContent, marketplaceIds, ct);

        logger.LogInformation("Amazon stock/price sync: {Count} ürün kontrol edildi, tenant {TenantId}",
            publishedProducts.Count, tenantId);
    }
}

using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Pttavm;
using Entegrasyon.Business.Tenants;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Her 15 dakikada PttAVM'deki yayinda urunlerin stok ve fiyat bilgilerini senkronize eder.
/// </summary>
public class PttavmStockPriceSyncService(
    IServiceScopeFactory scopeFactory,
    ITenantRegistry tenantRegistry,
    ILogger<PttavmStockPriceSyncService> logger)
    : TenantAwarePollingService(scopeFactory, tenantRegistry, logger)
{
    private const int PttavmMarketPlaceId = MarketPlaceConstants.PttavmMarketPlaceId;
    private const int MaxBatchSize = 1000;

    protected override TimeSpan PollInterval => TimeSpan.FromMinutes(15);

    protected override async Task PollForTenantAsync(
        IServiceProvider services, int tenantId,
        DateTimeOffset lastPoll, CancellationToken ct)
    {
        var dbContextFactory = services.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
        var stockPriceService = services.GetRequiredService<IPttavmStockPriceService>();

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        var publishedProducts = await dbContext.ProductMarketplaces
            .Where(pm => pm.MarketPlaceId == PttavmMarketPlaceId &&
                         pm.Status == MarketplaceProductStatus.Published &&
                         pm.ExternalProductId != null)
            .Include(pm => pm.Product)
                .ThenInclude(p => p.ProductVariants)
                    .ThenInclude(v => v.BranchOfficeStocks)
            .ToListAsync(ct);

        if (!publishedProducts.Any())
        {
            logger.LogDebug("PttAVM stock/price sync: yayında ürün yok, tenant {TenantId}", tenantId);
            return;
        }

        logger.LogInformation("PttAVM stock/price sync: {Count} ürün kontrol ediliyor, tenant {TenantId}",
            publishedProducts.Count, tenantId);

        var items = publishedProducts
            .SelectMany(pm => pm.Product.ProductVariants
                .Select(variant => new PttavmStockPriceRequest(
                    Barcode: variant.Barcode ?? pm.ExternalProductId!,
                    Active: true,
                    Quantity: variant.BranchOfficeStocks.Sum(s => s.CurrentStock),
                    PriceWithoutVat: variant.SalePrice,
                    PriceWithVat: variant.SalePrice * (1 + (variant.VatRate / 100m)),
                    VatRate: (int)variant.VatRate,
                    Discount: null,
                    IsCargoFromSupplier: null,
                    Variants: null)))
            .ToList();

        if (items.Count == 0)
        {
            logger.LogDebug("PttAVM stock/price sync: güncellenecek item yok, tenant {TenantId}", tenantId);
            return;
        }

        // Batch processing: max 1000/batch
        for (var i = 0; i < items.Count; i += MaxBatchSize)
        {
            var batch = items.GetRange(i, Math.Min(MaxBatchSize, items.Count - i));
            var result = await stockPriceService.UpdateStockPricesAsync(batch, ct);
            if (result.Success)
            {
                logger.LogInformation("PttAVM stock/price sync batch tamamlandı, tenant {TenantId}: {Count} item, trackingId={TrackingId}",
                    tenantId, batch.Count, result.Data?.TrackingId);
            }
            else
            {
                logger.LogWarning("PttAVM stock/price sync batch başarısız, tenant {TenantId}: {Message}",
                    tenantId, result.Message);
            }
        }
    }
}

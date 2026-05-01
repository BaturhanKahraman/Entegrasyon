using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events.Marketplace;
using Entegrasyon.Business.Channels.Events.Products;
using Entegrasyon.Business.Concrete.Pazarama;
using Entegrasyon.Business.FeatureFlags;
using Entegrasyon.Business.Tenants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Trendyol;
using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// StockPriceChangedEvent'leri tüketir ve Trendyol'a stok/fiyat güncelleme gönderir.
/// Yalnızca Trendyol'da yayında olan ürünlerin varyantları için çalışır.
/// Aynı event ile Pazarama sync de yapılır.
/// </summary>
public class TrendyolStockPriceSyncService(
    EventChannel<StockPriceChangedEvent> channel,
    IServiceScopeFactory scopeFactory,
    ILogger<TrendyolStockPriceSyncService> logger,
    IOptions<NotificationFeatureFlags> notificationFlags) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var evt in channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();

                // Tenant context initialize
                var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
                var tenantRegistry = scope.ServiceProvider.GetRequiredService<ITenantRegistry>();
                var tenant = await tenantRegistry.GetByIdAsync(evt.TenantId);
                if (tenant is null || !tenant.IsActive)
                {
                    logger.LogWarning("{Service}: Tenant {TenantId} not found or inactive, skipping event",
                        nameof(TrendyolStockPriceSyncService), evt.TenantId);
                    continue;
                }
                tenantContext.Initialize(tenant);

                await HandleTrendyolSyncAsync(scope.ServiceProvider, evt, stoppingToken, notificationFlags.Value);
                await HandlePazaramaSyncAsync(scope.ServiceProvider, evt, stoppingToken, notificationFlags.Value);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to sync stock/price for variant {VariantId}, TenantId={TenantId}",
                    evt.ProductVariantId, evt.TenantId);
            }
        }
    }

    private async Task HandleTrendyolSyncAsync(IServiceProvider services, StockPriceChangedEvent evt, CancellationToken stoppingToken, NotificationFeatureFlags flags)
    {
        var dbContext = services.GetRequiredService<IntegrationDbContext>();
        var stockPriceService = services.GetRequiredService<ITrendyolStockPriceService>();

        // Ürünün Trendyol'da yayında olup olmadığını kontrol et
        var pm = await dbContext.ProductMarketplaces.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProductId == evt.ProductId
                && x.MarketPlaceId == TrendyolMarketPlaceId
                && x.Status == MarketplaceProductStatus.Published, stoppingToken);

        if (pm is null)
        {
            logger.LogDebug("Product {ProductId} is not published on Trendyol, skipping stock/price sync",
                evt.ProductId);
            return;
        }

        // Varyantı çek — stok ve fiyat bilgisi
        var variant = await dbContext.ProductVariants.AsNoTracking()
            .Include(v => v.BranchOfficeStocks)
            .FirstOrDefaultAsync(v => v.Id == evt.ProductVariantId, stoppingToken);

        if (variant is null) return;

        // Marketplace'e stok gönderecek depo ID'leri
        var warehouseIds = await dbContext.MarketPlaceWarehouses.AsNoTracking()
            .Where(w => w.MarketPlaceId == TrendyolMarketPlaceId)
            .Select(w => w.BranchOfficeId)
            .ToListAsync(stoppingToken);

        if (warehouseIds.Count == 0)
        {
            warehouseIds = await dbContext.BranchOffices.AsNoTracking()
                .Where(b => b.IsDefaultMarketPlaceStock)
                .Select(b => b.Id)
                .ToListAsync(stoppingToken);
        }

        var quantity = variant.BranchOfficeStocks
            .Where(s => warehouseIds.Contains(s.BranchOfficeId))
            .Sum(s => s.CurrentStock);

        var salePrice = variant.SalePrice > variant.ListPrice ? variant.ListPrice : variant.SalePrice;

        var items = new List<TrendyolPriceInventoryItem>
        {
            new(variant.Barcode!, quantity, salePrice, variant.ListPrice)
        };

        var result = await stockPriceService.UpdatePriceAndInventoryAsync(items);

        if (result.Success)
            logger.LogInformation("Stock/price update sent to Trendyol for variant {Barcode}", variant.Barcode);
        else
        {
            logger.LogWarning("Stock/price update failed for variant {Barcode}: {Message}", variant.Barcode, result.Message);

            if (flags.PublishEnabled)
            {
                dbContext.AddDomainEvent(new MarketplaceStockSyncFailedEvent(
                    marketPlaceId: TrendyolMarketPlaceId,
                    productId: evt.ProductId,
                    error: result.Message ?? string.Empty));
                await dbContext.SaveChangesAsync(stoppingToken);
            }
        }
    }

    private async Task HandlePazaramaSyncAsync(IServiceProvider services, StockPriceChangedEvent evt, CancellationToken stoppingToken, NotificationFeatureFlags flags)
    {
        var dbContext = services.GetRequiredService<IntegrationDbContext>();
        var stockPriceService = services.GetRequiredService<IPazaramaStockPriceService>();

        // Ürünün Pazarama'da yayında olup olmadığını kontrol et
        var pm = await dbContext.ProductMarketplaces.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProductId == evt.ProductId
                && x.MarketPlaceId == PazaramaMarketPlaceId
                && x.Status == MarketplaceProductStatus.Published, stoppingToken);

        if (pm is null)
        {
            logger.LogDebug("Product {ProductId} is not published on Pazarama, skipping stock/price sync",
                evt.ProductId);
            return;
        }

        // Varyantı çek — stok ve fiyat bilgisi
        var variant = await dbContext.ProductVariants.AsNoTracking()
            .Include(v => v.BranchOfficeStocks)
            .FirstOrDefaultAsync(v => v.Id == evt.ProductVariantId, stoppingToken);

        if (variant is null) return;

        // Marketplace'e stok gönderecek depo ID'leri
        var warehouseIds = await dbContext.MarketPlaceWarehouses.AsNoTracking()
            .Where(w => w.MarketPlaceId == PazaramaMarketPlaceId)
            .Select(w => w.BranchOfficeId)
            .ToListAsync(stoppingToken);

        if (warehouseIds.Count == 0)
        {
            warehouseIds = await dbContext.BranchOffices.AsNoTracking()
                .Where(b => b.IsDefaultMarketPlaceStock)
                .Select(b => b.Id)
                .ToListAsync(stoppingToken);
        }

        var quantity = variant.BranchOfficeStocks
            .Where(s => warehouseIds.Contains(s.BranchOfficeId))
            .Sum(s => s.CurrentStock);

        var salePrice = variant.SalePrice > variant.ListPrice ? variant.ListPrice : variant.SalePrice;

        var stockItems = new List<PazaramaStockUpdateItem>
        {
            new(variant.Barcode!, quantity)
        };

        var priceItems = new List<PazaramaPriceUpdateItem>
        {
            new(variant.Barcode!, variant.ListPrice, salePrice)
        };

        var stockResult = await stockPriceService.UpdateStockAsync(stockItems);
        if (stockResult.Success)
            logger.LogInformation("Stock update sent to Pazarama for variant {Barcode}", variant.Barcode);
        else
        {
            logger.LogWarning("Stock update failed for Pazarama variant {Barcode}: {Message}", variant.Barcode, stockResult.Message);

            if (flags.PublishEnabled)
            {
                dbContext.AddDomainEvent(new MarketplaceStockSyncFailedEvent(
                    marketPlaceId: PazaramaMarketPlaceId,
                    productId: evt.ProductId,
                    error: stockResult.Message ?? string.Empty));
                await dbContext.SaveChangesAsync(stoppingToken);
            }
        }

        var priceResult = await stockPriceService.UpdatePriceAsync(priceItems);
        if (priceResult.Success)
            logger.LogInformation("Price update sent to Pazarama for variant {Barcode}", variant.Barcode);
        else
        {
            logger.LogWarning("Price update failed for Pazarama variant {Barcode}: {Message}", variant.Barcode, priceResult.Message);

            if (flags.PublishEnabled)
            {
                dbContext.AddDomainEvent(new MarketplacePriceUpdateFailedEvent(
                    marketPlaceId: PazaramaMarketPlaceId,
                    productId: evt.ProductId,
                    error: priceResult.Message ?? string.Empty));
                await dbContext.SaveChangesAsync(stoppingToken);
            }
        }
    }
}

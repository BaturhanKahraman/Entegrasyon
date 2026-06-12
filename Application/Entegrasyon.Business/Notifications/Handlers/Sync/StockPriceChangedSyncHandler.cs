using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Marketplace;
using Entegrasyon.Business.Channels.Events.Products;
using Entegrasyon.Business.FeatureFlags;
using Entegrasyon.Business.Tenants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Trendyol;
using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Business.Notifications.Handlers.Sync;

public sealed class StockPriceChangedSyncHandler(
    ITenantContext tenantContext,
    ITenantRegistry tenantRegistry,
    IDbContextFactory<IntegrationDbContext> dbContextFactory,
    ITrendyolStockPriceService trendyolStockPrice,
    IPazaramaStockPriceService pazaramaStockPrice,
    IOptions<NotificationFeatureFlags> notificationFlags,
    ILogger<StockPriceChangedSyncHandler> logger) : IDomainEventHandler<StockPriceChangedEvent>
{
    public async Task HandleAsync(StockPriceChangedEvent @event, CancellationToken ct = default)
    {
        var tenant = await tenantRegistry.GetByIdAsync(@event.TenantId);
        if (tenant is null || !tenant.IsActive)
        {
            logger.LogWarning("{Handler}: Tenant {TenantId} not found or inactive, skipping",
                nameof(StockPriceChangedSyncHandler), @event.TenantId);
            return;
        }
        tenantContext.Initialize(tenant);

        var flags = notificationFlags.Value;
        await HandleTrendyolAsync(@event, flags, ct);
        await HandlePazaramaAsync(@event, flags, ct);
    }

    private async Task HandleTrendyolAsync(StockPriceChangedEvent @event, NotificationFeatureFlags flags, CancellationToken ct)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);

        var pm = await db.ProductMarketplaces.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProductId == @event.ProductId
                && x.MarketPlaceId == TrendyolMarketPlaceId
                && x.Status == MarketplaceProductStatus.Published, ct);

        if (pm is null)
        {
            logger.LogDebug("Product {ProductId} not published on Trendyol, skipping stock/price sync", @event.ProductId);
            return;
        }

        var variant = await db.ProductVariants.AsNoTracking()
            .Include(v => v.BranchOfficeStocks)
            .FirstOrDefaultAsync(v => v.Id == @event.ProductVariantId, ct);

        if (variant is null) return;

        var warehouseIds = await db.MarketPlaceWarehouses.AsNoTracking()
            .Where(w => w.MarketPlaceId == TrendyolMarketPlaceId)
            .Select(w => w.BranchOfficeId)
            .ToListAsync(ct);

        if (warehouseIds.Count == 0)
            warehouseIds = await db.BranchOffices.AsNoTracking()
                .Where(b => b.IsDefaultMarketPlaceStock)
                .Select(b => b.Id)
                .ToListAsync(ct);

        var quantity = variant.BranchOfficeStocks
            .Where(s => warehouseIds.Contains(s.BranchOfficeId))
            .Sum(s => s.CurrentStock);

        var salePrice = variant.SalePrice > variant.ListPrice ? variant.ListPrice : variant.SalePrice;

        var result = await trendyolStockPrice.UpdatePriceAndInventoryAsync(
        [
            new TrendyolPriceInventoryItem(variant.Barcode!, quantity, salePrice, variant.ListPrice)
        ]);

        if (result.Success)
        {
            logger.LogInformation("Stock/price update sent to Trendyol for variant {Barcode}", variant.Barcode);
            return;
        }

        logger.LogWarning("Stock/price update failed for Trendyol variant {Barcode}: {Message}", variant.Barcode, result.Message);

        if (flags.PublishEnabled)
        {
            db.AddDomainEvent(new MarketplaceStockSyncFailedEvent(
                marketPlaceId: TrendyolMarketPlaceId,
                productId: @event.ProductId,
                error: result.Message ?? string.Empty));
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task HandlePazaramaAsync(StockPriceChangedEvent @event, NotificationFeatureFlags flags, CancellationToken ct)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);

        var pm = await db.ProductMarketplaces.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProductId == @event.ProductId
                && x.MarketPlaceId == PazaramaMarketPlaceId
                && x.Status == MarketplaceProductStatus.Published, ct);

        if (pm is null)
        {
            logger.LogDebug("Product {ProductId} not published on Pazarama, skipping stock/price sync", @event.ProductId);
            return;
        }

        var variant = await db.ProductVariants.AsNoTracking()
            .Include(v => v.BranchOfficeStocks)
            .FirstOrDefaultAsync(v => v.Id == @event.ProductVariantId, ct);

        if (variant is null) return;

        var warehouseIds = await db.MarketPlaceWarehouses.AsNoTracking()
            .Where(w => w.MarketPlaceId == PazaramaMarketPlaceId)
            .Select(w => w.BranchOfficeId)
            .ToListAsync(ct);

        if (warehouseIds.Count == 0)
            warehouseIds = await db.BranchOffices.AsNoTracking()
                .Where(b => b.IsDefaultMarketPlaceStock)
                .Select(b => b.Id)
                .ToListAsync(ct);

        var quantity = variant.BranchOfficeStocks
            .Where(s => warehouseIds.Contains(s.BranchOfficeId))
            .Sum(s => s.CurrentStock);

        var salePrice = variant.SalePrice > variant.ListPrice ? variant.ListPrice : variant.SalePrice;

        var stockResult = await pazaramaStockPrice.UpdateStockAsync(
            [new Concrete.Pazarama.PazaramaStockUpdateItem(variant.Barcode!, quantity)]);

        if (!stockResult.Success)
        {
            logger.LogWarning("Stock update failed for Pazarama variant {Barcode}: {Message}", variant.Barcode, stockResult.Message);
            if (flags.PublishEnabled)
            {
                db.AddDomainEvent(new Channels.Events.Marketplace.MarketplaceStockSyncFailedEvent(
                    marketPlaceId: PazaramaMarketPlaceId,
                    productId: @event.ProductId,
                    error: stockResult.Message ?? string.Empty));
                await db.SaveChangesAsync(ct);
            }
        }
        else
        {
            logger.LogInformation("Stock update sent to Pazarama for variant {Barcode}", variant.Barcode);
        }

        var priceResult = await pazaramaStockPrice.UpdatePriceAsync(
            [new Concrete.Pazarama.PazaramaPriceUpdateItem(variant.Barcode!, variant.ListPrice, salePrice)]);

        if (!priceResult.Success)
        {
            logger.LogWarning("Price update failed for Pazarama variant {Barcode}: {Message}", variant.Barcode, priceResult.Message);
            if (flags.PublishEnabled)
            {
                db.AddDomainEvent(new Channels.Events.Marketplace.MarketplacePriceUpdateFailedEvent(
                    marketPlaceId: PazaramaMarketPlaceId,
                    productId: @event.ProductId,
                    error: priceResult.Message ?? string.Empty));
                await db.SaveChangesAsync(ct);
            }
        }
        else
        {
            logger.LogInformation("Price update sent to Pazarama for variant {Barcode}", variant.Barcode);
        }
    }
}

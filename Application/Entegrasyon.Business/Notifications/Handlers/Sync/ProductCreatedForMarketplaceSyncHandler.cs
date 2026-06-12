using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Products;
using Entegrasyon.Business.Tenants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Notifications.Handlers.Sync;

public sealed class ProductCreatedForMarketplaceSyncHandler(
    ITenantContext tenantContext,
    ITenantRegistry tenantRegistry,
    IDbContextFactory<IntegrationDbContext> dbContextFactory,
    ITrendyolProductService trendyolProductService,
    IHepsiburadaProductService hepsiburadaProductService,
    IPazaramaProductService pazaramaProductService,
    ILogger<ProductCreatedForMarketplaceSyncHandler> logger) : IDomainEventHandler<ProductCreatedForMarketplaceEvent>
{
    public async Task HandleAsync(ProductCreatedForMarketplaceEvent @event, CancellationToken ct = default)
    {
        var tenant = await tenantRegistry.GetByIdAsync(@event.TenantId);
        if (tenant is null || !tenant.IsActive)
        {
            logger.LogWarning("{Handler}: Tenant {TenantId} not found or inactive, skipping",
                nameof(ProductCreatedForMarketplaceSyncHandler), @event.TenantId);
            return;
        }
        tenantContext.Initialize(tenant);

        logger.LogInformation("Processing marketplace publish for product {ProductId}: {Marketplaces}, TenantId={TenantId}",
            @event.ProductId, string.Join(", ", @event.Marketplaces), @event.TenantId);

        foreach (var marketplace in @event.Marketplaces)
        {
            switch (marketplace)
            {
                case "Trendyol":
                    await HandleTrendyolAsync(@event.ProductId, ct);
                    break;
                case "Hepsiburada":
                    await HandleHepsiburadaAsync(@event.ProductId, ct);
                    break;
                case "Pazarama":
                    await HandlePazaramaAsync(@event.ProductId, ct);
                    break;
                default:
                    logger.LogWarning("Unsupported marketplace: {Marketplace}", marketplace);
                    break;
            }
        }
    }

    private async Task HandleTrendyolAsync(Guid productId, CancellationToken ct)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);

        var record = await db.ProductMarketplaces.AsTracking()
            .FirstOrDefaultAsync(pm => pm.ProductId == productId && pm.MarketPlace.Name == "Trendyol", ct);

        if (record is null)
        {
            logger.LogWarning("ProductMarketplace record not found for ProductId={ProductId}, Trendyol", productId);
            return;
        }

        try
        {
            var result = await trendyolProductService.PublishProductAsync(productId);

            if (result.Success && !string.IsNullOrWhiteSpace(result.Data))
            {
                record.BatchRequestId = result.Data;
                logger.LogInformation("Product {ProductId} published to Trendyol. BatchId={BatchId}", productId, result.Data);
            }
            else if (result.Success)
            {
                record.Status = MarketplaceProductStatus.Failed;
                record.StatusMessage = "Trendyol API başarılı döndü ancak BatchRequestId boş geldi.";
                logger.LogWarning("Product {ProductId}: Trendyol publish succeeded but BatchRequestId is empty", productId);
            }
            else
            {
                record.Status = MarketplaceProductStatus.Failed;
                record.StatusMessage = result.Message;
                logger.LogWarning("Product {ProductId} failed to publish to Trendyol: {Message}", productId, result.Message);
            }
        }
        catch (Exception ex)
        {
            record.Status = MarketplaceProductStatus.Failed;
            record.StatusMessage = $"Publish isteği sırasında hata: {ex.Message}";
            logger.LogError(ex, "Exception during Trendyol publish for product {ProductId}", productId);
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task HandleHepsiburadaAsync(Guid productId, CancellationToken ct)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);

        var record = await db.ProductMarketplaces.AsTracking()
            .FirstOrDefaultAsync(pm => pm.ProductId == productId && pm.MarketPlace.Name == "Hepsiburada", ct);

        if (record is null)
        {
            logger.LogWarning("ProductMarketplace record not found for ProductId={ProductId}, Hepsiburada", productId);
            return;
        }

        try
        {
            var result = await hepsiburadaProductService.PublishProductAsync(productId);

            if (result.Success && !string.IsNullOrWhiteSpace(result.Data))
            {
                record.BatchRequestId = result.Data;
                logger.LogInformation("Product {ProductId} published to Hepsiburada. TrackingId={TrackingId}", productId, result.Data);
            }
            else if (result.Success)
            {
                record.Status = MarketplaceProductStatus.Failed;
                record.StatusMessage = "Hepsiburada API başarılı döndü ancak trackingId boş geldi.";
            }
            else
            {
                record.Status = MarketplaceProductStatus.Failed;
                record.StatusMessage = result.Message;
                logger.LogWarning("Product {ProductId} failed to publish to Hepsiburada: {Message}", productId, result.Message);
            }
        }
        catch (Exception ex)
        {
            record.Status = MarketplaceProductStatus.Failed;
            record.StatusMessage = $"Publish isteği sırasında hata: {ex.Message}";
            logger.LogError(ex, "Exception during Hepsiburada publish for product {ProductId}", productId);
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task HandlePazaramaAsync(Guid productId, CancellationToken ct)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);

        var record = await db.ProductMarketplaces.AsTracking()
            .FirstOrDefaultAsync(pm => pm.ProductId == productId && pm.MarketPlace.Name == "Pazarama", ct);

        if (record is null)
        {
            logger.LogWarning("ProductMarketplace record not found for ProductId={ProductId}, Pazarama", productId);
            return;
        }

        try
        {
            var result = await pazaramaProductService.PublishProductAsync(productId);

            if (result.Success && !string.IsNullOrWhiteSpace(result.Data))
            {
                record.BatchRequestId = result.Data;
                logger.LogInformation("Product {ProductId} published to Pazarama. BatchId={BatchId}", productId, result.Data);
            }
            else if (result.Success)
            {
                record.Status = MarketplaceProductStatus.Failed;
                record.StatusMessage = "Pazarama API başarılı döndü ancak BatchRequestId boş geldi.";
            }
            else
            {
                record.Status = MarketplaceProductStatus.Failed;
                record.StatusMessage = result.Message;
                logger.LogWarning("Product {ProductId} failed to publish to Pazarama: {Message}", productId, result.Message);
            }
        }
        catch (Exception ex)
        {
            record.Status = MarketplaceProductStatus.Failed;
            record.StatusMessage = $"Publish isteği sırasında hata: {ex.Message}";
            logger.LogError(ex, "Exception during Pazarama publish for product {ProductId}", productId);
        }

        await db.SaveChangesAsync(ct);
    }
}

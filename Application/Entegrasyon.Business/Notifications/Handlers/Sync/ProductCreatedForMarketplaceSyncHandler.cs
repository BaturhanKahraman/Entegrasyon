using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Products;
using Entegrasyon.Business.Tenants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Products;
using Entegrasyon.Entity.Results;
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

        Task LogUnsupported(string name)
        {
            logger.LogWarning("Unsupported marketplace: {Marketplace}", name);
            return Task.CompletedTask;
        }

        var tasks = @event.Marketplaces.Select(marketplace => marketplace switch
        {
            "Trendyol"    => HandleMarketplaceAsync(@event.ProductId, "Trendyol",    id => trendyolProductService.PublishProductAsync(id),    ct),
            "Hepsiburada" => HandleMarketplaceAsync(@event.ProductId, "Hepsiburada", id => hepsiburadaProductService.PublishProductAsync(id), ct),
            "Pazarama"    => HandleMarketplaceAsync(@event.ProductId, "Pazarama",    id => pazaramaProductService.PublishProductAsync(id),    ct),
            _             => LogUnsupported(marketplace)
        });

        await Task.WhenAll(tasks);
    }

    private async Task HandleMarketplaceAsync(
        Guid productId,
        string marketplaceName,
        Func<Guid, Task<IDataResult<string>>> publishFn,
        CancellationToken ct)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(ct);

        var record = await db.ProductMarketplaces.AsTracking()
            .FirstOrDefaultAsync(pm => pm.ProductId == productId && pm.MarketPlace.Name == marketplaceName, ct);

        if (record is null)
        {
            logger.LogWarning("ProductMarketplace record not found for ProductId={ProductId}, {Marketplace}", productId, marketplaceName);
            return;
        }

        try
        {
            var result = await publishFn(productId);

            if (result.Success && !string.IsNullOrWhiteSpace(result.Data))
            {
                record.BatchRequestId = result.Data;
                logger.LogInformation("Product {ProductId} published to {Marketplace}. BatchId={BatchId}", productId, marketplaceName, result.Data);
            }
            else if (result.Success)
            {
                record.Status = MarketplaceProductStatus.Failed;
                record.StatusMessage = $"{marketplaceName} API başarılı döndü ancak BatchRequestId boş geldi.";
                logger.LogWarning("Product {ProductId}: {Marketplace} publish succeeded but BatchRequestId is empty", productId, marketplaceName);
            }
            else
            {
                record.Status = MarketplaceProductStatus.Failed;
                record.StatusMessage = result.Message;
                logger.LogWarning("Product {ProductId} failed to publish to {Marketplace}: {Message}", productId, marketplaceName, result.Message);
            }
        }
        catch (Exception ex)
        {
            record.Status = MarketplaceProductStatus.Failed;
            record.StatusMessage = $"Publish isteği sırasında hata: {ex.Message}";
            logger.LogError(ex, "Exception during {Marketplace} publish for product {ProductId}", marketplaceName, productId);
        }

        await db.SaveChangesAsync(ct);
    }
}

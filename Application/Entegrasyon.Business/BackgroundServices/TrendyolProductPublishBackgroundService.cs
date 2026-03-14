using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events.Products;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// EventChannel'dan ProductCreatedForMarketplaceEvent okur ve ilgili pazaryeri API'sini çağırır.
/// ProductMarketplace kaydı zaten ProductSyncManager tarafından oluşturulmuş olmalı.
///
/// Akış:
///   ProductSyncManager → ProductMarketplace (Pending) + Event yayınla
///   Bu servis → Event al → ITrendyolProductService.PublishProductAsync → Durumu güncelle
///
/// TODO: ProductAddedEvent consumer'ı oluşturulmalı — ürün eklendikten sonra otomatik olarak
/// konfigüre edilmiş pazaryerlerine ProductCreatedForMarketplaceEvent yayınlamalı.
/// </summary>
public class TrendyolProductPublishBackgroundService(
    EventChannel<ProductCreatedForMarketplaceEvent> channel,
    IServiceScopeFactory scopeFactory,
    ILogger<TrendyolProductPublishBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var evt in channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                logger.LogInformation(
                    "Processing marketplace publish for product {ProductId}: {Marketplaces}",
                    evt.ProductId, string.Join(", ", evt.Marketplaces));

                await using var scope = scopeFactory.CreateAsyncScope();

                foreach (var marketplace in evt.Marketplaces)
                {
                    switch (marketplace)
                    {
                        case "Trendyol":
                            await HandleTrendyolAsync(scope.ServiceProvider, evt.ProductId, stoppingToken);
                            break;
                        default:
                            logger.LogWarning("Unsupported marketplace: {Marketplace}", marketplace);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to publish product {ProductId} to marketplace", evt.ProductId);
            }
        }
    }

    private async Task HandleTrendyolAsync(IServiceProvider services, Guid productId, CancellationToken ct)
    {
        var dbContext = services.GetRequiredService<IntegrationDbContext>();
        var trendyolService = services.GetRequiredService<ITrendyolProductService>();

        var record = await dbContext.ProductMarketplaces
            .FirstOrDefaultAsync(pm => pm.ProductId == productId &&
                                       pm.MarketPlace.Name == "Trendyol", ct);

        if (record is null)
        {
            logger.LogWarning("ProductMarketplace record not found for ProductId={ProductId}, Trendyol", productId);
            return;
        }

        var result = await trendyolService.PublishProductAsync(productId);

        if (result.Success)
        {
            record.BatchRequestId = result.Data;
            logger.LogInformation("Product {ProductId} published to Trendyol. BatchId={BatchId}", productId, result.Data);
        }
        else
        {
            record.Status = MarketplaceProductStatus.Failed;
            record.StatusMessage = result.Message;
            logger.LogWarning("Product {ProductId} failed to publish to Trendyol: {Message}", productId, result.Message);
        }

        await dbContext.SaveChangesAsync(ct);
    }
}

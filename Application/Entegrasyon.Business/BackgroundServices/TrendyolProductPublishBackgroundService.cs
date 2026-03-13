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
///   Bu servis → Event al → API çağır → Durumu Published/Failed güncelle
///
/// TODO: ProductAddedEvent consumer'ı oluşturulmalı — ürün eklendikten sonra otomatik olarak
/// konfigüre edilmiş pazaryerlerine ProductCreatedForMarketplaceEvent yayınlamalı.
/// Bu sayede AddProduct → ProductAddedEvent → [consumer] → ProductCreatedForMarketplaceEvent → bu servis
/// akışı ile ürünler otomatik sync kuyruğuna girecek.
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
                var dbContext = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();

                foreach (var marketplace in evt.Marketplaces)
                {
                    switch (marketplace)
                    {
                        case "Trendyol":
                            await HandleTrendyolAsync(dbContext, evt.ProductId, stoppingToken);
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

    private async Task HandleTrendyolAsync(IntegrationDbContext dbContext, Guid productId, CancellationToken ct)
    {
        var record = await dbContext.ProductMarketplaces
            .Include(pm => pm.Product)
            .FirstOrDefaultAsync(pm => pm.ProductId == productId &&
                                       pm.MarketPlace.Name == "Trendyol", ct);

        if (record is null)
        {
            logger.LogWarning("ProductMarketplace record not found for ProductId={ProductId}, Trendyol", productId);
            return;
        }

        // TODO: ITrendyolProductService.PublishProduct(productId) çağrılacak
        // Başarılı:
        //   record.Status = MarketplaceProductStatus.Published;
        //   record.LastSyncedAt = DateTimeOffset.UtcNow;
        //   record.BatchRequestId = response.BatchRequestId;
        // Başarısız:
        //   record.Status = MarketplaceProductStatus.Failed;
        //   record.StatusMessage = error.Message;
        // await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("Trendyol publish pending for ProductId={ProductId} — API integration not yet implemented", productId);
    }
}

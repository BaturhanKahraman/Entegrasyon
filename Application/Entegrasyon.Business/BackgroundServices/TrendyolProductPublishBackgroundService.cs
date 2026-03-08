using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Listens for ProductCreatedForMarketplaceEvent and publishes products to selected marketplaces.
/// Currently supports Trendyol. Other marketplaces are placeholders.
/// </summary>
public class TrendyolProductPublishBackgroundService : BackgroundService
{
    private readonly EventChannel<ProductCreatedForMarketplaceEvent> _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TrendyolProductPublishBackgroundService> _logger;

    public TrendyolProductPublishBackgroundService(
        EventChannel<ProductCreatedForMarketplaceEvent> channel,
        IServiceScopeFactory scopeFactory,
        ILogger<TrendyolProductPublishBackgroundService> logger)
    {
        _channel = channel;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var evt in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                _logger.LogInformation(
                    "Publishing product {ProductId} to marketplaces: {Marketplaces}",
                    evt.ProductId, string.Join(", ", evt.Marketplaces));

                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();

                foreach (var marketplace in evt.Marketplaces)
                {
                    switch (marketplace)
                    {
                        case "Trendyol":
                            await HandleTrendyolAsync(dbContext, evt.ProductId, stoppingToken);
                            break;
                        default:
                            _logger.LogWarning("Unsupported marketplace: {Marketplace}", marketplace);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish product {ProductId} to marketplace", evt.ProductId);
            }
        }
    }

    private async Task HandleTrendyolAsync(IntegrationDbContext dbContext, Guid productId, CancellationToken ct)
    {
        var trendyolMarketplace = await dbContext.MarketPlaces
            .FirstOrDefaultAsync(x => x.Name == "Trendyol", ct);

        if (trendyolMarketplace is null)
        {
            _logger.LogWarning("Trendyol marketplace not found in database.");
            return;
        }

        var existing = await dbContext.ProductMarketplaces
            .FirstOrDefaultAsync(x => x.ProductId == productId && x.MarketPlaceId == trendyolMarketplace.Id, ct);

        if (existing is null)
        {
            dbContext.ProductMarketplaces.Add(new ProductMarketplace
            {
                ProductId = productId,
                MarketPlaceId = trendyolMarketplace.Id,
                Status = MarketplaceProductStatus.Pending
            });
            await dbContext.SaveChangesAsync(ct);
            _logger.LogInformation("ProductMarketplace Pending record created for ProductId={ProductId}", productId);
        }

        // TODO: Inject and call ITrendyolProductService.PublishProduct(productId)
        _logger.LogInformation("Trendyol product publish queued for ProductId={ProductId}", productId);
    }
}

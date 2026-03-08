using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events;
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

                foreach (var marketplace in evt.Marketplaces)
                {
                    switch (marketplace)
                    {
                        case "Trendyol":
                            // TODO: Inject and call ITrendyolProductService.PublishProduct(evt.ProductId)
                                    _logger.LogInformation("Trendyol product publish queued for ProductId={ProductId}", evt.ProductId);
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
}

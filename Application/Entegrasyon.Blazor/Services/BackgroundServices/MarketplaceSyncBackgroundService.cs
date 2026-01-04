using Entegrasyon.Blazor.Services.Channels;
using Entegrasyon.Blazor.Services.Channels.Events;

namespace Entegrasyon.Blazor.Services.BackgroundServices;

/// <summary>
/// Background service that listens to marketplace sync events
/// </summary>
public class MarketplaceSyncBackgroundService : BackgroundService
{
    private readonly EventChannel<MarketplaceSyncEvent> _syncEventChannel;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MarketplaceSyncBackgroundService> _logger;

    public MarketplaceSyncBackgroundService(
        EventChannel<MarketplaceSyncEvent> syncEventChannel,
        IServiceProvider serviceProvider,
        ILogger<MarketplaceSyncBackgroundService> logger)
    {
        _syncEventChannel = syncEventChannel;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MarketplaceSyncBackgroundService started");

        await foreach (var evt in _syncEventChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                _logger.LogInformation(
                    "Processing marketplace sync: Marketplace={Marketplace}, Type={Type}, EntityId={EntityId}",
                    evt.MarketplaceName, evt.SyncType, evt.EntityId);

                await using var scope = _serviceProvider.CreateAsyncScope();

                // TODO: Implement actual marketplace sync logic based on SyncType
                // Example:
                // if (evt.SyncType == "Product")
                // {
                //     var productManager = scope.ServiceProvider.GetRequiredService<IProductManager>();
                //     await productManager.SyncToMarketplace(evt.EntityId.Value, evt.MarketplaceName);
                // }

                await Task.Delay(500, stoppingToken); // Simulate processing

                _logger.LogInformation(
                    "Marketplace sync completed: Marketplace={Marketplace}, Type={Type}",
                    evt.MarketplaceName, evt.SyncType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to process marketplace sync: Marketplace={Marketplace}, Type={Type}",
                    evt.MarketplaceName, evt.SyncType);
            }
        }
    }
}

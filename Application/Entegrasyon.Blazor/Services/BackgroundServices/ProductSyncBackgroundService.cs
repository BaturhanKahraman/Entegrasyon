using Entegrasyon.Blazor.Services.Channels;
using Entegrasyon.Blazor.Services.Channels.Events;

namespace Entegrasyon.Blazor.Services.BackgroundServices;

/// <summary>
/// Background service that listens to product update events and handles marketplace sync
/// Example of using Channels for event processing
/// </summary>
public class ProductSyncBackgroundService : BackgroundService
{
    private readonly EventChannel<ProductUpdatedEvent> _productEventChannel;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProductSyncBackgroundService> _logger;

    public ProductSyncBackgroundService(
        EventChannel<ProductUpdatedEvent> productEventChannel,
        IServiceProvider serviceProvider,
        ILogger<ProductSyncBackgroundService> logger)
    {
        _productEventChannel = productEventChannel;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ProductSyncBackgroundService started");

        await foreach (var evt in _productEventChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                _logger.LogInformation("Processing product event: ProductId={ProductId}, Action={Action}", 
                    evt.ProductId, evt.Action);

                await using var scope = _serviceProvider.CreateAsyncScope();
                
                // TODO: Get product manager and sync to marketplace
                // var productManager = scope.ServiceProvider.GetRequiredService<IProductManager>();
                // await productManager.SyncToMarketplace(evt.ProductId);

                await Task.Delay(100, stoppingToken); // Simulate processing
                
                _logger.LogInformation("Product event processed successfully: ProductId={ProductId}", 
                    evt.ProductId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process product event: ProductId={ProductId}", 
                    evt.ProductId);
                // Don't stop processing, continue with next event
            }
        }
    }
}

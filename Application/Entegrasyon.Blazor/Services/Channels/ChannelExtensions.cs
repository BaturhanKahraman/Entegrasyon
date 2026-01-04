using Entegrasyon.Blazor.Services.Channels.Events;

namespace Entegrasyon.Blazor.Services.Channels;

/// <summary>
/// Extension methods to register event channels in DI
/// </summary>
public static class ChannelExtensions
{
    public static IServiceCollection AddEventChannels(this IServiceCollection services)
    {
        // Register event channels as singletons
        services.AddSingleton<EventChannel<ProductUpdatedEvent>>();
        services.AddSingleton<EventChannel<CategoryUpdatedEvent>>();
        services.AddSingleton<EventChannel<OrderCreatedEvent>>();
        services.AddSingleton<EventChannel<MarketplaceSyncEvent>>();

        return services;
    }
}

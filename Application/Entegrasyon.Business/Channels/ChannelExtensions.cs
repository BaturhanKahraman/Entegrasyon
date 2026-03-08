using Entegrasyon.Business.Channels.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.Business.Channels;

public static class ChannelExtensions
{
    public static IServiceCollection AddEventChannels(this IServiceCollection services)
    {
        services.AddSingleton<EventChannel<ProductCreatedForMarketplaceEvent>>();
        services.AddSingleton<EventChannel<CategoryUpdatedEvent>>();
        services.AddSingleton<EventChannel<CategoryImportRequestedEvent>>();
        services.AddSingleton<EventChannel<CategoryImportCompletedEvent>>();
        services.AddSingleton<EventChannel<NotificationEvent>>();
        return services;
    }
}

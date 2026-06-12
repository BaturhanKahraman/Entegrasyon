using Entegrasyon.Business.Channels.Events.Categories;
using Entegrasyon.Business.Channels.Events.Chat;
using Microsoft.Extensions.DependencyInjection;

namespace Entegrasyon.Business.Channels;

public static class ChannelExtensions
{
    public static IServiceCollection AddEventChannels(this IServiceCollection services)
    {
        services.AddSingleton<EventChannel<CategoryUpdatedEvent>>();
        services.AddSingleton<EventChannel<CategoryImportRequestedEvent>>();
        services.AddSingleton<EventChannel<CategoryImportCompletedEvent>>();
        services.AddSingleton<EventChannel<ChatMessageEvent>>();
        return services;
    }
}

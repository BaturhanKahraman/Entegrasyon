using System.Threading.Channels;
using Entegrasyon.Business.Channels.Events;
using Entegrasyon.Business.Notifications.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Channels;

public sealed class InProcessEventDispatcher(
    Channel<BaseEvent> ephemeralChannel,
    IServiceScopeFactory scopeFactory,
    ILogger<InProcessEventDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var evt in ephemeralChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var evtType = evt.GetType();
                var handlerInterface = typeof(IDomainEventHandler<>).MakeGenericType(evtType);
                var handlers = scope.ServiceProvider.GetServices(handlerInterface);
                var method = handlerInterface.GetMethod("HandleAsync")!;
                foreach (var handler in handlers)
                    await (Task)method.Invoke(handler, [evt, stoppingToken])!;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ephemeral dispatch failed for {EventType}", evt.GetType().Name);
                // ephemeral — loss is acceptable
            }
        }
    }
}

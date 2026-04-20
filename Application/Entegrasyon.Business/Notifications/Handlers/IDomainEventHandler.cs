using Entegrasyon.Business.Channels.Events;

namespace Entegrasyon.Business.Notifications.Handlers;

public interface IDomainEventHandler<in TEvent> where TEvent : BaseEvent
{
    Task HandleAsync(TEvent @event, CancellationToken ct = default);
}

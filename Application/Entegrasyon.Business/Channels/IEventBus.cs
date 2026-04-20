using Entegrasyon.Business.Channels.Events;

namespace Entegrasyon.Business.Channels;

public interface IEventBus
{
    /// <summary>
    /// Ephemeral (UI-sync, non-durable) event'leri in-memory channel'a publish eder.
    /// Persistent event'ler için IntegrationDbContext.AddDomainEvent kullanın.
    /// </summary>
    ValueTask PublishAsync<TEvent>(TEvent @event, bool persistent = false, CancellationToken ct = default)
        where TEvent : BaseEvent;
}

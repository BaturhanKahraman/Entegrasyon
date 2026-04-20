using System.Reflection;
using System.Threading.Channels;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events;

namespace Entegrasyon.Business.Channels;

public sealed class InMemoryEventBus(
    Channel<BaseEvent> ephemeralChannel,
    ITenantContext tenantContext) : IEventBus
{
    private static readonly PropertyInfo TenantIdProperty =
        typeof(BaseEvent).GetProperty(nameof(BaseEvent.TenantId))!;

    public async ValueTask PublishAsync<TEvent>(TEvent @event, bool persistent = false, CancellationToken ct = default)
        where TEvent : BaseEvent
    {
        if (persistent)
        {
            throw new InvalidOperationException(
                "Persistent event'ler IEventBus yerine IntegrationDbContext.AddDomainEvent() ile " +
                "publish edilmelidir (business transaction ile atomiklik için).");
        }

        TenantIdProperty.SetValue(@event, tenantContext.TenantId);
        await ephemeralChannel.Writer.WriteAsync(@event, ct);
    }
}

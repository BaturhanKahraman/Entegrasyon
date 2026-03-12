namespace Entegrasyon.Business.Channels.Events;

public abstract class BaseEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

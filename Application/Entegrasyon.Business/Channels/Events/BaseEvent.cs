namespace Entegrasyon.Business.Channels.Events;

public abstract class BaseEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    /// <summary>
    /// Event'i ureten tenant'in ID'si.
    /// Publisher'lar ITenantContext.TenantId'den set eder.
    /// Consumer'lar bu deger ile tenant scope olusturur.
    /// </summary>
    public int TenantId { get; set; }
}

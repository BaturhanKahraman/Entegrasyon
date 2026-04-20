namespace Entegrasyon.Entity.Events;

/// <summary>
/// DataAccess katmanının domain event'lerle etkileşebilmesi için minimal arayüz.
/// BaseEvent bu interface'i implemente eder; IntegrationDbContext.AddDomainEvent
/// bu tip üzerinden çalışır.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
    int TenantId { get; set; }
}

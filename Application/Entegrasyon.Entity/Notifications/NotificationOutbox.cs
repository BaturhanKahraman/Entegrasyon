namespace Entegrasyon.Entity.Notifications;

public sealed class NotificationOutbox : BaseEntity
{
    public long Id { get; set; }
    public int TenantId { get; set; }
    public string EventType { get; set; } = null!;
    public string PayloadJson { get; set; } = null!;
    public OutboxStatus Status { get; set; } = OutboxStatus.Pending;
    public DateTimeOffset? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset? NextRetryAt { get; set; }
}

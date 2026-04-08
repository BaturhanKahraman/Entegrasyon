namespace Entegrasyon.Entity.Webhooks;

public sealed class WebhookSubscription : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Secret { get; set; }
    public string EventTypes { get; set; } = "[]";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastTriggeredAt { get; set; }
    public int FailureCount { get; set; }
}

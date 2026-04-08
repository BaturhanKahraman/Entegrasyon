namespace Entegrasyon.Entity.Webhooks;

public sealed class WebhookDeliveryLog : BaseEntity
{
    public long Id { get; set; }
    public int SubscriptionId { get; set; }
    public WebhookSubscription Subscription { get; set; } = null!;
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public int? ResponseCode { get; set; }
    public string? ResponseBody { get; set; }
    public bool Success { get; set; }
    public int RetryCount { get; set; }
}

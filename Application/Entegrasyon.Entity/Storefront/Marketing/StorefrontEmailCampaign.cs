namespace Entegrasyon.Entity.Storefront;

public sealed class StorefrontEmailCampaign : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Subject { get; set; } = null!;
    public string HtmlContent { get; set; } = null!;
    public CampaignStatus Status { get; set; }
    public CampaignTarget Target { get; set; }
    public int TotalRecipients { get; set; }
    public int SentCount { get; set; }
    public int OpenedCount { get; set; }
    public DateTimeOffset? ScheduledAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
}

public enum CampaignStatus { Draft, Scheduled, Sending, Sent, Cancelled }
public enum CampaignTarget { AllSubscribers, AllCustomers, ActiveCustomers }

namespace Entegrasyon.Business.FeatureFlags;

public sealed class NotificationFeatureFlags
{
    public const string SectionName = "Features:NotificationsV2";

    public bool PublishEnabled { get; set; }
    public bool SseEnabled { get; set; }
    public bool WebPushEnabled { get; set; }
}

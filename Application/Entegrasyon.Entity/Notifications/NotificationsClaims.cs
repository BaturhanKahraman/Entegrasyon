namespace Entegrasyon.Entity.Notifications;

public class NotificationsClaims
{
    public long NotificationId { get; set; }
    public Notification Notification { get; set; } = null!;
    public int ClaimId { get; set; }
}

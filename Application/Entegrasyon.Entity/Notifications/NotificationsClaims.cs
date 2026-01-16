using System.ComponentModel.DataAnnotations.Schema;
using Entegrasyon.Entity.User;

namespace Entegrasyon.Entity.Notifications;

public class NotificationsClaims
{
    public long NotificationId { get; set; }
    public Notification Notification { get; set; }
    public int ClaimId { get; set; }
}

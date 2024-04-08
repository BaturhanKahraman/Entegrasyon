using System.ComponentModel.DataAnnotations.Schema;
using Entegrasyon.Entity.User;

namespace Entegrasyon.Entity.Notifications;

public class NotificationsUsers
{
    public Guid ApplicationUserId { get; set; }
    public ApplicationUser ApplicationUser { get; set; }
    public long NotificationId { get; set; }
    public Notification Notification { get; set; }
}
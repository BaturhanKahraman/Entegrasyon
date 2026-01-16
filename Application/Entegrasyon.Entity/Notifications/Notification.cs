using System.ComponentModel.DataAnnotations;
using Entegrasyon.Entity.User;
using Shared.Entity;

namespace Entegrasyon.Entity.Notifications;

public sealed class Notification : BaseEntity
{
    public long Id { get; set; }
    public string Header { get; set; }
    public string Content { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset ReadAt { get; set; }
    public ICollection<NotificationsUsers> NotificationsUsers { get; set; }
    public ICollection<ApplicationUser> Users { get; set; } = [];
    public ICollection<NotificationsClaims> NotificationClaims { get; set; } = [];
}

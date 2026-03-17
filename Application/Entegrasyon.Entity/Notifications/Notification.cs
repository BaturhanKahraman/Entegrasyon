using System.ComponentModel.DataAnnotations;
using Entegrasyon.Entity.User;

namespace Entegrasyon.Entity.Notifications;

public sealed class Notification : BaseEntity
{
    public long Id { get; set; }
    public string Header { get; set; } = null!;
    public string Content { get; set; } = null!;
    public bool IsRead { get; set; }
    public DateTimeOffset ReadAt { get; set; }
    public NotificationSeverity Severity { get; set; }
    public NotificationCategory Category { get; set; }
    public string? ActionUrl { get; set; }
    public ICollection<NotificationsUsers> NotificationsUsers { get; set; } = [];
    public ICollection<ApplicationUser> Users { get; set; } = [];
    public ICollection<NotificationsClaims> NotificationClaims { get; set; } = [];
}

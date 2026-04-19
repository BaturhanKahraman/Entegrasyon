namespace Entegrasyon.Entity.Notifications;

public sealed class AdminPushSubscription : BaseEntity
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string Endpoint { get; set; } = null!;
    public string P256dhKey { get; set; } = null!;
    public string AuthKey { get; set; } = null!;
    public string? UserAgent { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
}

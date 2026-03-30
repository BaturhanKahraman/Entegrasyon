namespace Entegrasyon.Entity.Notifications;

public sealed class NotificationSetting : BaseEntity
{
    public int Id { get; set; }
    public Guid? UserId { get; set; }
    public bool EnableOrderNotifications { get; set; } = true;
    public bool EnableStockAlerts { get; set; } = true;
    public bool EnableMarketplaceSyncNotifications { get; set; } = true;
    public bool EnableEmailNotifications { get; set; }
    public bool EnableSmsNotifications { get; set; }
    public string? PhoneNumber { get; set; }
    public bool ShowSnackbar { get; set; } = true;
    public int SnackbarDurationSeconds { get; set; } = 5;
}

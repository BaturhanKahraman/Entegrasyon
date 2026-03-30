namespace Entegrasyon.Entity.Dtos.Notifications;

public sealed class UpdateNotificationSettingDto
{
    public bool EnableOrderNotifications { get; set; }
    public bool EnableStockAlerts { get; set; }
    public bool EnableMarketplaceSyncNotifications { get; set; }
    public bool EnableEmailNotifications { get; set; }
    public bool EnableSmsNotifications { get; set; }
    public string? PhoneNumber { get; set; }
    public bool ShowSnackbar { get; set; }
    public int SnackbarDurationSeconds { get; set; }
}

namespace Entegrasyon.Blazor.Utility.ToastrNotification;

public static class NotificationDictionary
{
    public static Dictionary<NotificationType, string> NotificationTypeDictionary = new(
        new List<KeyValuePair<NotificationType, string>>()
        {
            new(NotificationType.Error,"error"),
            new(NotificationType.Success,"success"),
            new(NotificationType.Info,"info"),
            new(NotificationType.Warning,"warning"),
        }
    );
    public static Dictionary<NotificationPosition,string> NotificationPositionDictionary = new(
        new List<KeyValuePair<NotificationPosition,string>>()
        {
            new(NotificationPosition.RightBot,"toast-bottom-right"),
            new(NotificationPosition.CenterBottom,"toast-bottom-center"),
            new(NotificationPosition.RightTop,"toast-top-right"),
        }
    );

}
namespace Entegrasyon.MVC.Features.Storefront.ViewModels;

public class PushNotificationsVm
{
    public int SubscriberCount { get; set; }

    public List<PushHistoryEntry> SentNotifications { get; set; } = [];

    public record PushHistoryEntry(string Title, string Body, int Reached, DateTimeOffset SentAt);
}

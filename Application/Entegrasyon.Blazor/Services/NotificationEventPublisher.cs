using Entegrasyon.Blazor.Services.Channels;
using Entegrasyon.Blazor.Services.Channels.Events;
using Entegrasyon.Blazor.Utility.Notifications;
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Blazor.Services;

public class NotificationEventPublisher : INotificationEventPublisher
{
    private readonly EventChannel<NotificationEvent> _notificationEventChannel;
    private readonly IBlazorNotificationSender _blazorNotificationSender;

    public NotificationEventPublisher(
        EventChannel<NotificationEvent> notificationEventChannel,
        IBlazorNotificationSender blazorNotificationSender)
    {
        _notificationEventChannel = notificationEventChannel;
        _blazorNotificationSender = blazorNotificationSender;

        // Subscribe to the sender's event
        _blazorNotificationSender.NotificationSent += OnNotificationSent;
    }

    private async void OnNotificationSent(Notification notification, Guid userId)
    {
        var notificationEvent = new NotificationEvent(
            notification.Id,
            notification.Header,
            notification.Content,
            new[] { userId });

        await _notificationEventChannel.PublishAsync(notificationEvent);
    }
}

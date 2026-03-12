using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events.Notifications;

namespace Entegrasyon.Blazor.Utility.Notifications;

/// <summary>
/// EventChannel&lt;NotificationEvent&gt; kanalını dinleyerek bildirimleri
/// IBlazorNotificationSender üzerinden Blazor bileşenlerine iletir.
/// </summary>
public sealed class NotificationEventPublisher
{
    private readonly EventChannel<NotificationEvent> _channel;
    private readonly IBlazorNotificationSender _sender;

    public NotificationEventPublisher(
        EventChannel<NotificationEvent> channel,
        IBlazorNotificationSender sender)
    {
        _channel = channel;
        _sender = sender;
    }
}

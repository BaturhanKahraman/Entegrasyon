using Entegrasyon.Business.Channels.Events.Notifications;

namespace Entegrasyon.Business.Notifications;

public interface INotificationDeliveryService
{
    void Subscribe(Guid userId, Func<NotificationEvent, Task> handler);
    void Unsubscribe(Guid userId, Func<NotificationEvent, Task> handler);
    Task DeliverAsync(NotificationEvent evt);
}

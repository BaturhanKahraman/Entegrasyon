using Entegrasyon.Entity.Notifications;
using Entegrasyon.Business.Notifications;

namespace Entegrasyon.Business.Abstract;

public interface INotificationManager
{
    Task SendNotification(Notification notification, IEnumerable<SenderType> senderTypes);
    Task<IEnumerable<Notification>> GetNotificationsForUser(Guid userId, bool onlyUnread = false);
    Task MarkAsRead(long notificationId, Guid userId);
}

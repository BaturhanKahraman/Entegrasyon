using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Notifications;

public interface INotificationSender
{
    SenderType Type { get; }
    Task SendNotification(Notification message, IEnumerable<Guid> userIds);
}
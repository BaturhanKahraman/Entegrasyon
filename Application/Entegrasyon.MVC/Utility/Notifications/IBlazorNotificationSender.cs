using Entegrasyon.Business.Notifications;
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.MVC.Utility.Notifications;

public interface IBlazorNotificationSender:INotificationSender
{
    public event Action<Notification, Guid> NotificationSent;

}
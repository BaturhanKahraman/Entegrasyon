using Entegrasyon.Business.Notifications;
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Blazor.Utility.Notifications;

public interface IBlazorNotificationSender:INotificationSender
{
    public event Action<Notification, Guid> NotificationSent;

}
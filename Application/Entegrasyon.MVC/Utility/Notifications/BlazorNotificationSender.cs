using Entegrasyon.Business.Notifications;
using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.MVC.Utility.Notifications;

public sealed class BlazorNotificationSender : IBlazorNotificationSender
{
    public SenderType Type => SenderType.RealTime;
    public event Action<Notification, Guid> NotificationSent;

    public Task SendNotification(Notification message, IEnumerable<Guid> userIds)
    {
        foreach (var userId in userIds)
        {
            OnNotificationSent(message,userId);
        }

        return Task.CompletedTask;
    }

    private void OnNotificationSent(Notification arg1, Guid arg2)
    {
        NotificationSent?.Invoke(arg1, arg2);
    }
}
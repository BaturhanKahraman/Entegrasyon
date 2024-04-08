using Entegrasyon.Entity.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace Entegrasyon.Business.Notifications.SignalR;

public class NotificationHub : Hub
{
    public async Task SendNotificationToUser(Guid userId, Notification notification)
    {
        await Task.Yield();
    }
}
using Entegrasyon.Entity.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace Entegrasyon.Business.Notifications.SignalR;

public class SignalRSender(IHubContext<NotificationHub> hubContext) : ISignalRNotificationSender
{
    public SenderType Type => SenderType.SignalR;

    public Task SendNotification(Notification message, IEnumerable<Guid> userIds)
    {
        throw new NotImplementedException();
    }

    public async Task SendNotification(Notification notification, Guid userId)
    {
        await hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification",notification);
    }

    public async Task SendNotification(Notification notification)
    {
        await hubContext.Clients.All.SendAsync("ReceiveNotification", notification);
    }
}
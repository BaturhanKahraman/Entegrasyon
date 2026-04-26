using System.Net;
using System.Text.Json;
using Entegrasyon.Entity.Notifications;
using Lib.Net.Http.WebPush;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Notifications.WebPush;

public sealed class AdminWebPushSender(
    IAdminPushSubscriptionManager subscriptionManager,
    PushServiceClient pushClient,
    ILogger<AdminWebPushSender> logger) : INotificationSender
{
    public SenderType Type => SenderType.WebPushAdmin;

    public async Task SendNotification(Notification message, IEnumerable<Guid> userIds)
    {
        var payloadJson = JsonSerializer.Serialize(new
        {
            title = message.Header,
            body = message.Content,
            actionUrl = message.ActionUrl,
            notificationId = message.Id
        });

        foreach (var userId in userIds)
        {
            var subs = await subscriptionManager.GetByUserAsync(userId);
            foreach (var sub in subs)
            {
                try
                {
                    var pushSub = new PushSubscription();
                    pushSub.Endpoint = sub.Endpoint;
                    pushSub.SetKey(PushEncryptionKeyName.P256DH, sub.P256dhKey);
                    pushSub.SetKey(PushEncryptionKeyName.Auth, sub.AuthKey);
                    var pushMsg = new PushMessage(payloadJson)
                    {
                        Topic = $"notification-{message.Id}",
                        Urgency = PushMessageUrgency.Normal
                    };
                    await pushClient.RequestPushMessageDeliveryAsync(pushSub, pushMsg);
                }
                catch (PushServiceClientException ex) when (ex.StatusCode == HttpStatusCode.Gone)
                {
                    await subscriptionManager.UnsubscribeAsync(sub.Endpoint);
                    logger.LogInformation("Removed stale push subscription {Endpoint}", sub.Endpoint);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Push failed to {Endpoint}", sub.Endpoint);
                }
            }
        }
    }
}

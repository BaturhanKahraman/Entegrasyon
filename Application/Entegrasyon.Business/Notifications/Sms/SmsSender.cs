using Entegrasyon.Entity.Notifications;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Notifications.Sms;

public class SmsSender(ILogger<SmsSender> logger) : ISmsSender
{
    public SenderType Type => SenderType.Sms;

    public Task SendNotification(Notification message, IEnumerable<Guid> userIds)
    {
        logger.LogWarning("SMS sender is not configured yet");
        return Task.CompletedTask;
    }
}

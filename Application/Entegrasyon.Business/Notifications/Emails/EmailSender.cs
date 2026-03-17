using Entegrasyon.Entity.Notifications;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Notifications.Emails;

public class EmailSender(ILogger<EmailSender> logger) : IEmailSender
{
    public SenderType Type => SenderType.Email;

    public Task SendNotification(Notification message, IEnumerable<Guid> userIds)
    {
        logger.LogWarning("Email sending is not configured. Skipping batch notification: {Title}", message.Header);
        return Task.CompletedTask;
    }

    public Task SendNotification(Notification message, Guid userId)
    {
        logger.LogWarning("Email sending is not configured. Skipping notification to user {UserId}: {Title}", userId, message.Header);
        return Task.CompletedTask;
    }
}
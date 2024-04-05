using Entegrasyon.Entity.Notifications;

namespace Entegrasyon.Business.Notifications.Emails;

public class EmailSender:IEmailSender
{
    public SenderType Type => SenderType.Email;
    public Task SendNotification(Notification message, IEnumerable<Guid> userIds)
    {
        throw new NotImplementedException();
    }

    public Task SendNotification(Notification message, Guid userId)
    {
        throw new NotImplementedException();
    }

}
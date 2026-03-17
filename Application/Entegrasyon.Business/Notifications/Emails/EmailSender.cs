using System.Net;
using System.Net.Mail;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Notifications.Emails;

public class EmailSender(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    ILogger<EmailSender> logger) : IEmailSender
{
    public SenderType Type => SenderType.Email;

    public async Task SendNotification(Notification message, IEnumerable<Guid> userIds)
    {
        throw new NotImplementedException();
    }

    public Task SendNotification(Notification message, Guid userId)
    {
        throw new NotImplementedException();
    }

}

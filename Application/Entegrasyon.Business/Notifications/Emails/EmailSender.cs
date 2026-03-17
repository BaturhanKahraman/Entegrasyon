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
        var userIdList = userIds.ToList();
        foreach (var userId in userIdList)
        {
            await SendNotification(message, userId);
        }
    }

    public async Task SendNotification(Notification message, Guid userId)
    {
        using var dbContext = contextFactory.CreateDbContext();

        var settings = await dbContext.ApplicationSettings
            .Where(s => s.Group == "E-posta Ayarlar\u0131")
            .ToDictionaryAsync(s => s.Key, s => s.Value);

        if (!bool.TryParse(settings.GetValueOrDefault("SmtpNotificationEmailEnabled", "false"), out var enabled) || !enabled)
        {
            logger.LogDebug("Bildirim e-postalari devre disi, gonderilmedi");
            return;
        }

        var host = settings.GetValueOrDefault("SmtpHost", "");
        if (string.IsNullOrWhiteSpace(host))
        {
            logger.LogWarning("SMTP sunucu adresi yapilandirilmamis, e-posta gonderilemedi");
            return;
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.Email is null)
        {
            logger.LogWarning("Kullanici {UserId} icin e-posta adresi bulunamadi", userId);
            return;
        }

        try
        {
            var port = int.TryParse(settings.GetValueOrDefault("SmtpPort", "587"), out var p) ? p : 587;
            var enableSsl = bool.TryParse(settings.GetValueOrDefault("SmtpEnableSsl", "true"), out var ssl) && ssl;
            var fromAddress = settings.GetValueOrDefault("SmtpFromAddress", "");
            var fromName = settings.GetValueOrDefault("SmtpFromDisplayName", "Entegrasyon");
            var username = settings.GetValueOrDefault("SmtpUsername", "");
            var password = settings.GetValueOrDefault("SmtpPassword", "");

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                Timeout = 30000
            };

            if (!string.IsNullOrWhiteSpace(username))
                client.Credentials = new NetworkCredential(username, password);

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromAddress, fromName),
                Subject = message.Header ?? "Bildirim",
                Body = message.Content ?? "",
                IsBodyHtml = false
            };
            mailMessage.To.Add(user.Email);

            await client.SendMailAsync(mailMessage);
            logger.LogInformation("E-posta gonderildi: {Email}, Konu: {Subject}", user.Email, message.Header);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "E-posta gonderilemedi: {Email}", user.Email);
        }
    }
}

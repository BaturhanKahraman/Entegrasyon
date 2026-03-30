using System.Net;
using System.Net.Mail;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Notifications.Emails;

public class EmailSender(
    IApplicationSettingManager settingManager,
    IDbContextFactory<IntegrationDbContext> dbContextFactory,
    ILogger<EmailSender> logger) : IEmailSender
{
    public SenderType Type => SenderType.Email;

    public async Task SendNotification(Notification message, IEnumerable<Guid> userIds)
    {
        try
        {
            var userIdList = userIds.ToList();
            if (userIdList.Count == 0)
                return;

            // Load SMTP settings
            var settingDtos = await settingManager.GetSettingsByGroupAsync("E-posta Ayarları");
            var settings = settingDtos.ToDictionary(s => s.Key, s => s.Value);

            var host = settings.GetValueOrDefault("SmtpHost", "");
            var port = int.TryParse(settings.GetValueOrDefault("SmtpPort", "587"), out var p) ? p : 587;
            var enableSsl = settings.GetValueOrDefault("SmtpEnableSsl", "true") == "true";
            var username = settings.GetValueOrDefault("SmtpUsername", "");
            var password = settings.GetValueOrDefault("SmtpPassword", "");
            var fromAddress = settings.GetValueOrDefault("SmtpFromAddress", "");
            var fromName = settings.GetValueOrDefault("SmtpFromDisplayName", "Entegrasyon");

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromAddress))
            {
                logger.LogWarning("SMTP ayarları yapılandırılmamış. Email bildirimi gönderilemedi");
                return;
            }

            // Query users and their notification preferences
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            var users = await dbContext.Users
                .Where(u => userIdList.Contains(u.Id) && !u.IsDeleted && u.IsActive)
                .Select(u => new { u.Id, u.Email, u.FullName })
                .ToListAsync();

            var notificationSettings = await dbContext.NotificationSettings
                .Where(ns => ns.UserId.HasValue && userIdList.Contains(ns.UserId.Value))
                .ToDictionaryAsync(ns => ns.UserId!.Value);

            // Build email content
            var subject = message.Header ?? "Bildirim";
            var htmlBody = BuildNotificationEmailBody(message);

            using var smtpClient = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = enableSsl
            };

            foreach (var user in users)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(user.Email))
                    {
                        logger.LogDebug("Kullanıcının email adresi yok, atlanıyor: {UserId}", user.Id);
                        continue;
                    }

                    // Check user's email notification preference
                    if (notificationSettings.TryGetValue(user.Id, out var setting) && !setting.EnableEmailNotifications)
                    {
                        logger.LogDebug("Kullanıcı email bildirimlerini devre dışı bırakmış: {UserId}", user.Id);
                        continue;
                    }

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(fromAddress, fromName),
                        Subject = subject,
                        Body = htmlBody,
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(user.Email);

                    await smtpClient.SendMailAsync(mailMessage);
                    logger.LogInformation("Email bildirimi gönderildi: {Email}, Konu: {Subject}", user.Email, subject);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Email bildirimi gönderilemedi: {UserId}, {Email}", user.Id, user.Email);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Email bildirim süreci başarısız oldu");
        }
    }

    public Task SendNotification(Notification message, Guid userId)
    {
        return SendNotification(message, [userId]);
    }

    private static string BuildNotificationEmailBody(Notification notification)
    {
        var header = WebUtility.HtmlEncode(notification.Header ?? "Bildirim");
        var content = WebUtility.HtmlEncode(notification.Content ?? "");

        var severityColor = notification.Severity switch
        {
            NotificationSeverity.Error => "#DC2626",
            NotificationSeverity.Warning => "#F59E0B",
            NotificationSeverity.Success => "#16A34A",
            _ => "#2563EB"
        };

        return $"""
            <!DOCTYPE html>
            <html><head><meta charset="utf-8"></head>
            <body style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:20px;color:#333;">
                <div style="border-bottom:2px solid {severityColor};padding-bottom:16px;margin-bottom:24px;">
                    <h1 style="color:{severityColor};margin:0;font-size:24px;">Entegrasyon</h1>
                </div>
                <h2 style="margin-top:0;">{header}</h2>
                <p>{content}</p>
                <div style="border-top:1px solid #eee;padding-top:16px;margin-top:32px;color:#999;font-size:12px;">
                    <p>Bu email Entegrasyon sistemi tarafından otomatik olarak gönderilmiştir.</p>
                </div>
            </body></html>
            """;
    }
}

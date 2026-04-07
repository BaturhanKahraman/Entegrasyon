using System.Net;
using System.Net.Mail;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Storefront;

public class StorefrontEmailService(
    IApplicationSettingManager settingManager,
    ILogger<StorefrontEmailService> logger) : IStorefrontEmailService
{
    public async Task<IResult> SendEmailVerificationAsync(string toEmail, string customerName, string verificationToken, string storeName, string domain)
    {
        var verifyUrl = $"https://{domain}/email-dogrula?token={Uri.EscapeDataString(verificationToken)}";
        var subject = $"Email Adresinizi Dogrulayin - {storeName}";
        var body = BuildTemplate(storeName, $@"
            <h2>Merhaba {Encode(customerName)},</h2>
            <p>{Encode(storeName)}'e hos geldiniz! Email adresinizi dogrulamak icin asagidaki butona tiklayin:</p>
            <p style='text-align:center;margin:30px 0;'>
                <a href='{verifyUrl}' style='background-color:#2563EB;color:white;padding:12px 32px;text-decoration:none;border-radius:8px;font-weight:bold;'>Email Adresimi Dogrula</a>
            </p>
            <p style='color:#666;font-size:14px;'>Bu link 24 saat gecerlidir. Eger bu istegi siz yapmadiysiniz, bu emaili gormezden gelebilirsiniz.</p>");
        return await SendAsync(toEmail, subject, body);
    }

    public async Task<IResult> SendPasswordResetAsync(string toEmail, string customerName, string resetToken, string storeName, string domain)
    {
        var resetUrl = $"https://{domain}/sifre-sifirla?token={Uri.EscapeDataString(resetToken)}";
        var subject = $"Sifre Sifirlama - {storeName}";
        var body = BuildTemplate(storeName, $@"
            <h2>Merhaba {Encode(customerName)},</h2>
            <p>Sifrenizi sifirlamak icin asagidaki butona tiklayin:</p>
            <p style='text-align:center;margin:30px 0;'>
                <a href='{resetUrl}' style='background-color:#2563EB;color:white;padding:12px 32px;text-decoration:none;border-radius:8px;font-weight:bold;'>Sifremi Sifirla</a>
            </p>
            <p style='color:#666;font-size:14px;'>Bu link 1 saat gecerlidir. Eger bu istegi siz yapmadiysiniz, bu emaili gormezden gelebilirsiniz.</p>");
        return await SendAsync(toEmail, subject, body);
    }

    public async Task<IResult> SendWelcomeAsync(string toEmail, string customerName, string storeName)
    {
        var subject = $"Hos Geldiniz - {storeName}";
        var body = BuildTemplate(storeName, $@"
            <h2>Merhaba {Encode(customerName)},</h2>
            <p>{Encode(storeName)}'e hos geldiniz! Hesabiniz basariyla olusturuldu.</p>
            <p>Alisverise baslamak icin sitemizi ziyaret edebilirsiniz.</p>");
        return await SendAsync(toEmail, subject, body);
    }

    public async Task<IResult> SendOrderConfirmationAsync(string toEmail, string customerName, string orderNumber, decimal total, string storeName, string domain)
    {
        var orderUrl = $"https://{domain}/hesabim/Siparişler";
        var subject = $"Sipariş Onayiniz #{orderNumber} - {storeName}";
        var body = BuildTemplate(storeName, $@"
            <h2>Merhaba {Encode(customerName)},</h2>
            <p>Sipaarisiniz basariyla alindi!</p>
            <table style='width:100%;border-collapse:collapse;margin:20px 0;'>
                <tr><td style='padding:8px;border-bottom:1px solid #eee;color:#666;'>Sipariş No:</td><td style='padding:8px;border-bottom:1px solid #eee;font-weight:bold;'>#{Encode(orderNumber)}</td></tr>
                <tr><td style='padding:8px;border-bottom:1px solid #eee;color:#666;'>Toplam:</td><td style='padding:8px;border-bottom:1px solid #eee;font-weight:bold;'>{total:N2} TL</td></tr>
            </table>
            <p style='text-align:center;margin:30px 0;'>
                <a href='{orderUrl}' style='background-color:#2563EB;color:white;padding:12px 32px;text-decoration:none;border-radius:8px;font-weight:bold;'>Siparişlerimi Gor</a>
            </p>");
        return await SendAsync(toEmail, subject, body);
    }

    public async Task<IResult> SendAsync(string toEmail, string subject, string htmlBody)
    {
        try
        {
            var settingDtos = await settingManager.GetSettingsByGroupAsync("E-posta Ayarlar\u0131");
            var settings = settingDtos.ToDictionary(s => s.Key, s => s.Value);

            var host = settings.GetValueOrDefault("SmtpHost", "");
            var port = int.TryParse(settings.GetValueOrDefault("SmtpPort", "587"), out var p) ? p : 587;
            var enableSsl = settings.GetValueOrDefault("SmtpEnableSsl", "true") == "true";
            var username = settings.GetValueOrDefault("SmtpUsername", "");
            var password = settings.GetValueOrDefault("SmtpPassword", "");
            var fromAddress = settings.GetValueOrDefault("SmtpFromAddress", "");
            var fromName = settings.GetValueOrDefault("SmtpFromDisplayName", "Entegrasyon");

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(fromAddress))
            {
                logger.LogWarning("SMTP ayarlari yapilandirilmamis. Email gonderilemedi: {To}", toEmail);
                return new ErrorResult("SMTP ayarlari yapilandirilmamis.");
            }

            using var smtpClient = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = enableSsl
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromAddress, fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);
            logger.LogInformation("Email gonderildi: {To}, Konu: {Subject}", toEmail, subject);
            return new SuccessResult("Email gonderildi.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Email gonderilemedi: {To}, Konu: {Subject}", toEmail, subject);
            return new ErrorResult($"Email gonderilemedi: {ex.Message}");
        }
    }

    public async Task<IResult> SendAbandonedCartReminderAsync(string toEmail, string customerName, string storeName, string domain, int step, List<string> productNames, string? couponCode)
    {
        var cartUrl = $"https://{domain}/sepet";
        var productList = string.Join(", ", productNames);
        var stepSubjects = new Dictionary<int, string>
        {
            { 1, $"Sepetinizdeki urunler sizi bekliyor - {storeName}" },
            { 2, $"Sepetinizdeki urunleri unutmayin! - {storeName}" },
            { 3, $"Ozel indirim! Sepetinizdeki urunler - {storeName}" }
        };
        var subject = stepSubjects.GetValueOrDefault(step, stepSubjects[1]);

        var couponHtml = !string.IsNullOrEmpty(couponCode)
            ? $"<div style='background:#f0fdf4;border:1px solid #22c55e;border-radius:8px;padding:16px;margin:20px 0;text-align:center;'><p style='margin:0;color:#166534;font-size:14px;'>Ozel indirim kodunuz:</p><p style='margin:8px 0 0;font-size:24px;font-weight:bold;color:#15803d;letter-spacing:2px;'>{Encode(couponCode)}</p></div>"
            : "";

        var body = BuildTemplate(storeName, $@"
            <h2>Merhaba {Encode(customerName)},</h2>
            <p>Sepetinizde birakmis oldugunuz urunler hala sizin icin bekliyor:</p>
            <p style='font-weight:bold;color:#333;'>{Encode(productList)}</p>
            {couponHtml}
            <p style='text-align:center;margin:30px 0;'>
                <a href='{cartUrl}' style='background-color:#2563EB;color:white;padding:12px 32px;text-decoration:none;border-radius:8px;font-weight:bold;'>Sepetime Git</a>
            </p>");
        return await SendAsync(toEmail, subject, body);
    }

    private static string BuildTemplate(string storeName, string content)
    {
        return $@"<!DOCTYPE html>
<html><head><meta charset='utf-8'></head>
<body style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:20px;color:#333;'>
    <div style='border-bottom:2px solid #2563EB;padding-bottom:16px;margin-bottom:24px;'>
        <h1 style='color:#2563EB;margin:0;font-size:24px;'>{Encode(storeName)}</h1>
    </div>
    {content}
    <div style='border-top:1px solid #eee;padding-top:16px;margin-top:32px;color:#999;font-size:12px;'>
        <p>Bu email {Encode(storeName)} tarafindan gonderilmistir.</p>
    </div>
</body></html>";
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}

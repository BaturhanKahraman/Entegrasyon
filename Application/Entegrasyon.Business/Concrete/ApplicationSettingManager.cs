using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Settings;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete;

public sealed class ApplicationSettingManager(
    IDbContextFactory<IntegrationDbContext> contextFactory) : IApplicationSettingManager
{
    public async Task<List<ApplicationSettingDto>> GetAllSettingsAsync()
    {
        using var dbContext = contextFactory.CreateDbContext();
        var settings = await dbContext.ApplicationSettings
            .OrderBy(s => s.Group)
            .ThenBy(s => s.Id)
            .ToListAsync();
        return settings.Adapt<List<ApplicationSettingDto>>();
    }

    public async Task<List<ApplicationSettingDto>> GetSettingsByGroupAsync(string group)
    {
        using var dbContext = contextFactory.CreateDbContext();
        var settings = await dbContext.ApplicationSettings
            .Where(s => s.Group == group)
            .OrderBy(s => s.Id)
            .ToListAsync();
        return settings.Adapt<List<ApplicationSettingDto>>();
    }

    public async Task<ApplicationSettingDto?> GetSettingAsync(string key)
    {
        using var dbContext = contextFactory.CreateDbContext();
        var setting = await dbContext.ApplicationSettings
            .FirstOrDefaultAsync(s => s.Key == key);
        return setting?.Adapt<ApplicationSettingDto>();
    }

    public async Task<bool> UpdateSettingsAsync(List<UpdateApplicationSettingDto> settings)
    {
        using var dbContext = contextFactory.CreateDbContext();
        var ids = settings.Select(s => s.Id).ToList();
        var entities = await dbContext.ApplicationSettings
            .Where(s => ids.Contains(s.Id))
            .ToListAsync();

        foreach (var entity in entities)
        {
            var update = settings.First(s => s.Id == entity.Id);
            entity.Value = update.Value;
        }

        dbContext.ApplicationSettings.UpdateRange(entities);
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> TestSmtpConnectionAsync()
    {
        using var dbContext = contextFactory.CreateDbContext();
        var settings = await dbContext.ApplicationSettings
            .Where(s => s.Group == "E-posta Ayarları")
            .ToDictionaryAsync(s => s.Key, s => s.Value);

        var host = settings.GetValueOrDefault("SmtpHost", "");
        var port = int.TryParse(settings.GetValueOrDefault("SmtpPort", "587"), out var p) ? p : 587;
        var enableSsl = bool.TryParse(settings.GetValueOrDefault("SmtpEnableSsl", "true"), out var ssl) && ssl;

        if (string.IsNullOrWhiteSpace(host))
            throw new InvalidOperationException("SMTP sunucu adresi yapılandırılmamış.");

        using var client = new System.Net.Mail.SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            Timeout = 10000
        };

        var username = settings.GetValueOrDefault("SmtpUsername", "");
        var password = settings.GetValueOrDefault("SmtpPassword", "");
        if (!string.IsNullOrWhiteSpace(username))
            client.Credentials = new System.Net.NetworkCredential(username, password);

        using var tcpClient = new System.Net.Sockets.TcpClient();
        await tcpClient.ConnectAsync(host, port);
        return tcpClient.Connected;
    }

    public async Task SendTestEmailAsync()
    {
        using var dbContext = contextFactory.CreateDbContext();
        var settings = await dbContext.ApplicationSettings
            .Where(s => s.Group == "E-posta Ayarları")
            .ToDictionaryAsync(s => s.Key, s => s.Value);

        var host = settings.GetValueOrDefault("SmtpHost", "");
        if (string.IsNullOrWhiteSpace(host))
            throw new InvalidOperationException("SMTP sunucu adresi yapılandırılmamış.");

        var port = int.TryParse(settings.GetValueOrDefault("SmtpPort", "587"), out var p) ? p : 587;
        var enableSsl = bool.TryParse(settings.GetValueOrDefault("SmtpEnableSsl", "true"), out var ssl) && ssl;
        var fromAddress = settings.GetValueOrDefault("SmtpFromAddress", "");
        var fromName = settings.GetValueOrDefault("SmtpFromDisplayName", "Entegrasyon");
        var username = settings.GetValueOrDefault("SmtpUsername", "");
        var password = settings.GetValueOrDefault("SmtpPassword", "");

        if (string.IsNullOrWhiteSpace(fromAddress))
            throw new InvalidOperationException("Gönderen e-posta adresi yapılandırılmamış.");

        using var client = new System.Net.Mail.SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            Timeout = 30000
        };

        if (!string.IsNullOrWhiteSpace(username))
            client.Credentials = new System.Net.NetworkCredential(username, password);

        var mailMessage = new System.Net.Mail.MailMessage
        {
            From = new System.Net.Mail.MailAddress(fromAddress, fromName),
            Subject = "Entegrasyon - SMTP Test E-postası",
            Body = $"Bu bir test e-postasıdır. SMTP ayarlarınız doğru yapılandırılmış.\n\nGönderim zamanı: {DateTimeOffset.Now:dd.MM.yyyy HH:mm:ss}",
            IsBodyHtml = false
        };
        mailMessage.To.Add(fromAddress);

        await client.SendMailAsync(mailMessage);
    }
}

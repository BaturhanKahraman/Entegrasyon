using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Mappers;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Settings;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete;

public sealed class ApplicationSettingManager(
    IDbContextFactory<IntegrationDbContext> contextFactory) : IApplicationSettingManager
{
    public async Task<List<ApplicationSettingDto>> GetAllSettingsAsync()
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var settings = await dbContext.ApplicationSettings
            .OrderBy(s => s.Group)
            .ThenBy(s => s.Id)
            .ToListAsync();
        return SettingMapper.MapToDtoList(settings);
    }

    public async Task<List<ApplicationSettingDto>> GetSettingsByGroupAsync(string group)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var settings = await dbContext.ApplicationSettings
            .Where(s => s.Group == group)
            .OrderBy(s => s.Id)
            .ToListAsync();
        return SettingMapper.MapToDtoList(settings);
    }

    public async Task<ApplicationSettingDto?> GetSettingAsync(string key)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var setting = await dbContext.ApplicationSettings
            .FirstOrDefaultAsync(s => s.Key == key);
        return setting is null ? null : SettingMapper.MapToDto(setting);
    }

    public async Task<bool> UpdateSettingsAsync(List<UpdateApplicationSettingDto> settings)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
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
        await using var dbContext = await contextFactory.CreateDbContextAsync();
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
        await using var dbContext = await contextFactory.CreateDbContextAsync();
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

    public async Task<List<MarketPlace>> GetMarketplacesAsync()
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        return await dbContext.MarketPlaces
            .Where(m => !m.IsDeleted)
            .OrderBy(m => m.Id)
            .ToListAsync();
    }

    public async Task<bool> UpdateMarketplaceAsync(int id, string? apiKey, string? apiSecret, string? sellerId, string? baseUrl, string? tokenUrl, string? refreshToken)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var mp = await dbContext.MarketPlaces.AsTracking().FirstOrDefaultAsync(m => m.Id == id);
        if (mp is null) return false;

        mp.ApiKey = apiKey;
        mp.ApiSecret = apiSecret;
        mp.SellerId = sellerId;
        mp.BaseUrl = baseUrl;
        mp.TokenUrl = tokenUrl;
        mp.RefreshToken = refreshToken;

        await dbContext.SaveChangesAsync();
        return true;
    }
}

using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Settings;

namespace Entegrasyon.Business.Abstract;

public interface IApplicationSettingManager
{
    Task<List<ApplicationSettingDto>> GetAllSettingsAsync();
    Task<List<ApplicationSettingDto>> GetSettingsByGroupAsync(string group);
    Task<ApplicationSettingDto?> GetSettingAsync(string key);
    Task<bool> UpdateSettingsAsync(List<UpdateApplicationSettingDto> settings);
    Task<bool> TestSmtpConnectionAsync();
    Task SendTestEmailAsync();
    Task<List<MarketPlace>> GetMarketplacesAsync();
    Task<bool> UpdateMarketplaceAsync(int id, string? apiKey, string? apiSecret, string? sellerId, string? baseUrl, string? tokenUrl, string? refreshToken);
}

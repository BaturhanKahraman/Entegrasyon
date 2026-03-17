using Entegrasyon.Entity.Dtos.Settings;

namespace Entegrasyon.Business.Abstract;

public interface IApplicationSettingManager
{
    Task<List<ApplicationSettingDto>> GetAllSettingsAsync();
    Task<List<ApplicationSettingDto>> GetSettingsByGroupAsync(string group);
    Task<ApplicationSettingDto?> GetSettingAsync(string key);
    Task<bool> UpdateSettingsAsync(List<UpdateApplicationSettingDto> settings);
}

using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Settings;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Concrete;

public sealed class BarcodeScannerService(
    IApplicationSettingManager settingManager) : IBarcodeScannerService
{
    public async Task<BarcodeScannerConfig> GetConfigAsync(CancellationToken ct = default)
    {
        var enabledSetting = await settingManager.GetSettingAsync("BarcodeScanner.Enabled");
        var timeoutSetting = await settingManager.GetSettingAsync("BarcodeScanner.Timeout");
        var minLengthSetting = await settingManager.GetSettingAsync("BarcodeScanner.MinLength");
        var defaultActionSetting = await settingManager.GetSettingAsync("BarcodeScanner.DefaultAction");

        return new BarcodeScannerConfig
        {
            Enabled = enabledSetting is not null
                ? bool.TryParse(enabledSetting.Value, out var enabled) && enabled
                : true,
            Timeout = timeoutSetting is not null
                ? int.TryParse(timeoutSetting.Value, out var timeout) ? timeout : 100
                : 100,
            MinLength = minLengthSetting is not null
                ? int.TryParse(minLengthSetting.Value, out var minLength) ? minLength : 6
                : 6,
            DefaultAction = defaultActionSetting?.Value ?? "SalesAdd"
        };
    }

    public async Task<IResult> UpdateConfigAsync(BarcodeScannerConfig config, CancellationToken ct = default)
    {
        var settings = await settingManager.GetSettingsByGroupAsync("Barkod Okuyucu");
        if (settings.Count == 0)
            return new Result(false, "Barkod okuyucu ayarları bulunamadı.");

        var updates = new List<UpdateApplicationSettingDto>();

        foreach (var setting in settings)
        {
            var value = setting.Key switch
            {
                "BarcodeScanner.Enabled" => config.Enabled.ToString().ToLowerInvariant(),
                "BarcodeScanner.Timeout" => config.Timeout.ToString(),
                "BarcodeScanner.MinLength" => config.MinLength.ToString(),
                "BarcodeScanner.DefaultAction" => config.DefaultAction,
                _ => null
            };

            if (value is not null)
            {
                updates.Add(new UpdateApplicationSettingDto
                {
                    Id = setting.Id,
                    Value = value
                });
            }
        }

        var success = await settingManager.UpdateSettingsAsync(updates);
        return new Result(success, success ? "Ayarlar güncellendi." : "Ayarlar güncellenemedi.");
    }
}

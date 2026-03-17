using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Settings;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Settings;

public partial class GeneralSettings : ComponentBase
{
    [Inject] private IApplicationSettingManager SettingManager { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = default!;

    private Dictionary<string, List<ApplicationSettingDto>> _settingsByGroup = new();
    private Dictionary<string, bool> _showPasswords = new();
    private bool _loading = true;
    private bool _saving;
    private bool _testingSmtp;
    private bool _sendingTestEmail;

    protected override async Task OnInitializedAsync()
    {
        await LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        _loading = true;
        var settings = await SettingManager.GetAllSettingsAsync();
        _settingsByGroup = settings
            .GroupBy(s => s.Group ?? "Diğer")
            .OrderBy(g => g.Key switch
            {
                "Görünüm" => 0,
                "E-posta Ayarları" => 2,
                _ => 1
            })
            .ToDictionary(g => g.Key, g => g.ToList());
        _loading = false;
    }

    private async Task SaveSettingsAsync()
    {
        _saving = true;
        try
        {
            var allSettings = _settingsByGroup.Values.SelectMany(s => s).ToList();
            var updates = allSettings.Select(s => new UpdateApplicationSettingDto
            {
                Id = s.Id,
                Value = s.Value
            }).ToList();

            var success = await SettingManager.UpdateSettingsAsync(updates);
            if (success)
            {
                Snackbar.Add("Ayarlar başarıyla kaydedildi.", Severity.Success);
                await SyncThemeModeToLocalStorageAsync();
            }
            else
            {
                Snackbar.Add("Ayarlar kaydedilirken hata oluştu.", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Hata: {ex.Message}", Severity.Error);
        }
        finally
        {
            _saving = false;
        }
    }

    private async Task SyncThemeModeToLocalStorageAsync()
    {
        var themeSetting = _settingsByGroup.Values
            .SelectMany(s => s)
            .FirstOrDefault(s => s.Key == "ThemeMode");

        if (themeSetting is not null)
        {
            await JsRuntime.InvokeVoidAsync("localStorage.setItem", "themeMode", themeSetting.Value);
            // data-theme attribute'unu da güncelle
            var isDark = themeSetting.Value == "dark" ||
                (themeSetting.Value == "system" &&
                 await JsRuntime.InvokeAsync<bool>("eval", "window.matchMedia('(prefers-color-scheme: dark)').matches"));
            var attr = isDark ? "dark" : "light";
            await JsRuntime.InvokeVoidAsync("eval", $"document.documentElement.setAttribute('data-theme', '{attr}')");
        }
    }

    private void TogglePasswordVisibility(string key)
    {
        if (_showPasswords.ContainsKey(key))
            _showPasswords[key] = !_showPasswords[key];
        else
            _showPasswords[key] = true;
    }

    private async Task TestSmtpConnectionAsync()
    {
        _testingSmtp = true;
        try
        {
            await SaveSettingsAsync();

            var result = await SettingManager.TestSmtpConnectionAsync();
            if (result)
                Snackbar.Add("SMTP bağlantısı başarılı!", Severity.Success);
            else
                Snackbar.Add("SMTP bağlantısı başarısız.", Severity.Error);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"SMTP bağlantı hatası: {ex.Message}", Severity.Error);
        }
        finally
        {
            _testingSmtp = false;
        }
    }

    private async Task SendTestEmailAsync()
    {
        _sendingTestEmail = true;
        try
        {
            await SaveSettingsAsync();

            await SettingManager.SendTestEmailAsync();
            Snackbar.Add("Test e-postası gönderildi! Gelen kutunuzu kontrol edin.", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Test e-postası gönderilemedi: {ex.Message}", Severity.Error);
        }
        finally
        {
            _sendingTestEmail = false;
        }
    }
}

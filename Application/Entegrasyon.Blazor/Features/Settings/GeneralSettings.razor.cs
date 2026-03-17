using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Settings;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Settings;

public partial class GeneralSettings : ComponentBase
{
    [Inject] private IApplicationSettingManager SettingManager { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

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
            .OrderBy(g => g.Key == "E-posta Ayarları" ? 1 : 0)
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
                Snackbar.Add("Ayarlar başarıyla kaydedildi.", Severity.Success);
            else
                Snackbar.Add("Ayarlar kaydedilirken hata oluştu.", Severity.Error);
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

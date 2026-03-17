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
    private bool _loading = true;
    private bool _saving;

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
}

using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Notifications;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Settings;

public partial class NotificationSettings : ComponentBase
{
    [Inject] private INotificationSettingManager SettingManager { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [CascadingParameter] private Task<AuthenticationState> AuthStateTask { get; set; } = null!;

    private Guid _userId;
    private bool _loading = true;
    private bool _saving;
    private bool _sendingTest;

    private bool _enableOrderNotifications;
    private bool _enableStockAlerts;
    private bool _enableMarketplaceSyncNotifications;
    private bool _enableEmailNotifications;
    private bool _enableSmsNotifications;
    private bool _showSnackbar;
    private int _snackbarDurationSeconds;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateTask;
        var userIdClaim = authState.User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out _userId))
            return;

        await LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        _loading = true;
        var setting = await SettingManager.GetOrCreateForUserAsync(_userId);
        _enableOrderNotifications = setting.EnableOrderNotifications;
        _enableStockAlerts = setting.EnableStockAlerts;
        _enableMarketplaceSyncNotifications = setting.EnableMarketplaceSyncNotifications;
        _enableEmailNotifications = setting.EnableEmailNotifications;
        _showSnackbar = setting.ShowSnackbar;
        _snackbarDurationSeconds = setting.SnackbarDurationSeconds;
        _loading = false;
    }

    private async Task SaveSettingsAsync()
    {
        _saving = true;
        try
        {
            var dto = new UpdateNotificationSettingDto
            {
                EnableOrderNotifications = _enableOrderNotifications,
                EnableStockAlerts = _enableStockAlerts,
                EnableMarketplaceSyncNotifications = _enableMarketplaceSyncNotifications,
                EnableEmailNotifications = _enableEmailNotifications,
                ShowSnackbar = _showSnackbar,
                SnackbarDurationSeconds = _snackbarDurationSeconds
            };
            await SettingManager.UpdateAsync(_userId, dto);
            Snackbar.Add("Bildirim ayarları kaydedildi.", Severity.Success);
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

    private async Task SendTestNotificationAsync()
    {
        _sendingTest = true;
        try
        {
            await SettingManager.SendTestNotificationAsync(_userId);
            Snackbar.Add("Deneme bildirimi gönderildi!", Severity.Info);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Deneme bildirimi gönderilemedi: {ex.Message}", Severity.Error);
        }
        finally
        {
            _sendingTest = false;
        }
    }
}

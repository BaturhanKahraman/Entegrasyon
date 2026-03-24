using Entegrasyon.Desktop.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Desktop.Components.Pages;

public partial class SettingsPage
{
    [Inject] private SettingsService _settingsService { get; set; } = default!;
    [Inject] private SyncService _syncService { get; set; } = default!;
    [Inject] private OfflineSaleService _saleService { get; set; } = default!;
    [Inject] private PrintAgentHostedService _printAgentService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private PosSettings _settings = new();
    private bool _saving;
    private int _productCount;
    private int _pendingCount;

    protected override async Task OnInitializedAsync()
    {
        _settings = _settingsService.Settings;
        _productCount = await _saleService.GetProductCountAsync();
        _pendingCount = await _syncService.GetPendingCountAsync();
    }

    private async Task SaveSettings()
    {
        _saving = true;

        try
        {
            await _settingsService.SaveAsync();
            Snackbar.Add("Ayarlar kaydedildi", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Kaydetme hatasi: {ex.Message}", Severity.Error);
        }
        finally
        {
            _saving = false;
        }
    }

    private async Task StartPrintAgent()
    {
        try
        {
            await _printAgentService.StartAsync();
            Snackbar.Add($"PrintAgent baslatildi (port {_printAgentService.Port})", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"PrintAgent baslatma hatasi: {ex.Message}", Severity.Error);
        }
    }

    private async Task StopPrintAgent()
    {
        try
        {
            await _printAgentService.StopAsync();
            Snackbar.Add("PrintAgent durduruldu", Severity.Info);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"PrintAgent durdurma hatasi: {ex.Message}", Severity.Error);
        }
    }
}

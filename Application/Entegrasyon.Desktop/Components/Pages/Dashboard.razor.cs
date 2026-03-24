using System.Globalization;
using Entegrasyon.Desktop.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Desktop.Components.Pages;

public partial class Dashboard
{
    [Inject] private SyncService _syncService { get; set; } = default!;
    [Inject] private OfflineSaleService _saleService { get; set; } = default!;
    [Inject] private PrintAgentHostedService _printAgentService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private DailySalesSummary _summary = new(0, 0, 0, 0);
    private int _pendingCount;
    private int _productCount;
    private bool _syncing;

    private static readonly CultureInfo _trCulture = new("tr-TR");

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        _summary = await _saleService.GetTodaySummaryAsync();
        _pendingCount = await _syncService.GetPendingCountAsync();
        _productCount = await _saleService.GetProductCountAsync();
    }

    private async Task ManualSync()
    {
        _syncing = true;
        StateHasChanged();

        try
        {
            var result = await _syncService.FullSyncAsync();
            if (result.Success)
                Snackbar.Add($"Senkronizasyon tamamlandi: {result.Message}", Severity.Success);
            else
                Snackbar.Add($"Senkronizasyon hatasi: {result.Message}", Severity.Error);

            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Sync hatasi: {ex.Message}", Severity.Error);
        }
        finally
        {
            _syncing = false;
        }
    }
}

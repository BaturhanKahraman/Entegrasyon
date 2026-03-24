using Entegrasyon.Desktop.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Desktop.Components.Pages;

public partial class SyncPage
{
    [Inject] private SyncService _syncService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private int _pendingCount;
    private bool _syncing;
    private readonly List<SyncLogEntry> _syncLogs = [];

    protected override async Task OnInitializedAsync()
    {
        _pendingCount = await _syncService.GetPendingCountAsync();
    }

    private async Task RunFullSync()
    {
        _syncing = true;
        StateHasChanged();

        try
        {
            var result = await _syncService.FullSyncAsync();
            AddLog(result);
            _pendingCount = await _syncService.GetPendingCountAsync();

            Snackbar.Add(result.Success
                ? $"Senkronizasyon tamamlandi: {result.Message}"
                : $"Sync hatasi: {result.Message}",
                result.Success ? Severity.Success : Severity.Error);
        }
        catch (Exception ex)
        {
            AddLog(new SyncResult(false, ex.Message));
            Snackbar.Add($"Sync hatasi: {ex.Message}", Severity.Error);
        }
        finally
        {
            _syncing = false;
        }
    }

    private async Task PullProducts()
    {
        _syncing = true;
        StateHasChanged();

        try
        {
            var result = await _syncService.PullProductsAsync();
            AddLog(result);
            Snackbar.Add(result.Message, result.Success ? Severity.Success : Severity.Warning);
        }
        finally
        {
            _syncing = false;
        }
    }

    private async Task PushSales()
    {
        _syncing = true;
        StateHasChanged();

        try
        {
            var result = await _syncService.PushSalesAsync();
            AddLog(result);
            _pendingCount = await _syncService.GetPendingCountAsync();
            Snackbar.Add(result.Message, result.Success ? Severity.Success : Severity.Warning);
        }
        finally
        {
            _syncing = false;
        }
    }

    private void AddLog(SyncResult result)
    {
        _syncLogs.Add(new SyncLogEntry(DateTimeOffset.Now, result.Success, result.Message));
    }

    private record SyncLogEntry(DateTimeOffset Timestamp, bool Success, string Message);
}

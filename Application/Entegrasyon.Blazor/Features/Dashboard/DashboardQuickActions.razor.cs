using Entegrasyon.Business.Abstract;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Dashboard;

public partial class DashboardQuickActions
{
    [Inject] private IProductSyncManager ProductSyncManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    [Parameter] public EventCallback OnSyncCompleted { get; set; }

    private async Task SyncMarketplaces()
    {
        Snackbar.Add("Pazaryeri senkronizasyonu başlatıldı", Severity.Info);
        var result = await ProductSyncManager.SyncAllPendingAsync(1);
        if (result.Success)
        {
            Snackbar.Add("Senkronizasyon tamamlandı", Severity.Success);
            await OnSyncCompleted.InvokeAsync();
        }
        else
        {
            Snackbar.Add($"Senkronizasyon hatası: {result.Message}", Severity.Error);
        }
    }
}

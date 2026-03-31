using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Product;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.MarketplaceSync.ProductSync;

public partial class HepsiburadaProductDetail : ComponentBase
{
    [Parameter, EditorRequired] public Guid ProductId { get; set; }
    [Parameter, EditorRequired] public MarketplaceSyncItemDto SyncItem { get; set; } = null!;
    [Parameter] public EventCallback OnSyncCompleted { get; set; }

    [Inject] private IProductSyncManager SyncManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private bool _isBusy;

    private async Task HandleSync()
    {
        _isBusy = true;
        try
        {
            var result = await SyncManager.SyncProductAsync(ProductId, marketPlaceId: 3);
            Snackbar.Add(result.Message ?? "", result.Success ? Severity.Success : Severity.Error);
            if (result.Success)
                await OnSyncCompleted.InvokeAsync();
        }
        finally
        {
            _isBusy = false;
        }
    }
}

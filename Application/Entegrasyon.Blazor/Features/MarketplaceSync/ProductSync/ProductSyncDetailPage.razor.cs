using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Product;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.MarketplaceSync.ProductSync;

public partial class ProductSyncDetailPage
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private IProductSyncManager SyncManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    private ProductSyncDetailDto? _detail;
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadDetail();
    }

    private async Task LoadDetail()
    {
        _loading = true;
        var result = await SyncManager.GetProductSyncDetailAsync(Id);
        if (result.Success && result.Data is not null)
            _detail = result.Data;
        _loading = false;
    }

    private async Task SyncToMarketplace(int marketPlaceId)
    {
        try
        {
            var result = await SyncManager.SyncProductAsync(Id, marketPlaceId);
            Snackbar.Add(result.Message ?? "", result.Success ? Severity.Success : Severity.Error);
            if (result.Success) await LoadDetail();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Hata: {ex.Message}", Severity.Error);
        }
    }

    private async Task RetryMarketplace(int marketPlaceId)
    {
        try
        {
            var result = await SyncManager.RetryFailedAsync(Id, marketPlaceId);
            Snackbar.Add(result.Message ?? "", result.Success ? Severity.Success : Severity.Error);
            if (result.Success) await LoadDetail();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Hata: {ex.Message}", Severity.Error);
        }
    }

    private void GoBack() => NavigationManager.NavigateTo("/marketplace/matching");
}

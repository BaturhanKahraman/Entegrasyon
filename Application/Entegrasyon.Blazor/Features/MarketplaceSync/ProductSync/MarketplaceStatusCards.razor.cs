using Entegrasyon.Entity.Dtos.Product;
using Microsoft.AspNetCore.Components;

namespace Entegrasyon.Blazor.Features.MarketplaceSync.ProductSync;

public partial class MarketplaceStatusCards : ComponentBase
{
    [Parameter, EditorRequired] public IReadOnlyList<MarketplaceSyncItemDto> Marketplaces { get; set; } = [];
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public EventCallback<int> OnMarketplaceSelected { get; set; }
    [Parameter] public Guid ProductId { get; set; }

    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    private async Task HandleCardClick(int marketPlaceId)
    {
        var mp = Marketplaces.FirstOrDefault(m => m.MarketPlaceId == marketPlaceId);
        if (mp is null || !mp.HasCredentials)
            return;

        // NeverSynced ise send sayfasına git
        if (mp.SyncState == MarketplaceSyncState.NeverSynced)
        {
            if (marketPlaceId == 1) // Trendyol
                NavigationManager.NavigateTo($"/products/{ProductId}/sync/trendyol/send");
            else
                await OnMarketplaceSelected.InvokeAsync(marketPlaceId);
        }
        else
        {
            await OnMarketplaceSelected.InvokeAsync(marketPlaceId);
        }
    }
}

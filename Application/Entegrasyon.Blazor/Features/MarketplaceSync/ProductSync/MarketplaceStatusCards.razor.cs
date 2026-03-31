using Entegrasyon.Entity.Dtos.Product;
using Microsoft.AspNetCore.Components;

namespace Entegrasyon.Blazor.Features.MarketplaceSync.ProductSync;

public partial class MarketplaceStatusCards : ComponentBase
{
    [Parameter, EditorRequired] public IReadOnlyList<MarketplaceSyncItemDto> Marketplaces { get; set; } = [];
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public EventCallback<int> OnMarketplaceSelected { get; set; }
}

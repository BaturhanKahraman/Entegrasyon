using Entegrasyon.Entity.Dtos.Product;
using Microsoft.AspNetCore.Components;

namespace Entegrasyon.Blazor.Features.MarketplaceSync.ProductSync;

public partial class ProductSyncSummaryCards
{
    [Parameter] public ProductSyncSummaryDto? Summary { get; set; }
    [Parameter] public bool IsLoading { get; set; }
}

using Entegrasyon.Entity.Dtos.Product.Marketplace;
using Microsoft.AspNetCore.Components;

namespace Entegrasyon.Blazor.Features.Products;

public partial class VariantPriceOverrideGrid
{
    [Parameter] public List<VariantPriceOverrideDetailDto> Variants { get; set; } = [];
}

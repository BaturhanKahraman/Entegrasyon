using Entegrasyon.Entity.Dtos.Branches;
using Microsoft.AspNetCore.Components;
using static Entegrasyon.Blazor.Features.Products.ProductEdit;

namespace Entegrasyon.Blazor.Features.Products;

public partial class ProductEditVariantsTab
{
    [Parameter] public List<VariantPriceModel> Variants { get; set; } = [];
    [Parameter] public List<BranchSelectDto> BranchOffices { get; set; } = [];
}

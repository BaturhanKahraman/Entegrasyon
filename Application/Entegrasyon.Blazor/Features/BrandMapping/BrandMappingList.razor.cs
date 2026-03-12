using Entegrasyon.Entity.Dtos.Brand;
using Microsoft.AspNetCore.Components;

namespace Entegrasyon.Blazor.Features.BrandMapping;

public partial class BrandMappingList : ComponentBase
{
    [Parameter]
    public List<BrandMarketPlaceMatchDto> Mappings { get; set; } = [];

    [Parameter]
    public EventCallback<BrandMarketPlaceMatchDto> OnDelete { get; set; }

    public async Task HandleDeleteClick(BrandMarketPlaceMatchDto mapping)
    {
        await OnDelete.InvokeAsync(mapping);
    }
}

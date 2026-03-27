using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Dtos.Marketplace;
using Microsoft.AspNetCore.Components;

namespace Entegrasyon.Blazor.Features.MarketplaceSync;

public partial class BrandMappingListPanel : ComponentBase
{
    [Parameter] public List<BrandDto> Brands { get; set; } = [];
    [Parameter] public List<BrandMarketPlaceMatchDto> AllMappings { get; set; } = [];
    [Parameter] public List<MarketplaceOption> Marketplaces { get; set; } = [];
    [Parameter] public BrandDto? SelectedBrand { get; set; }
    [Parameter] public EventCallback<BrandDto> SelectedBrandChanged { get; set; }

    private string _searchText = string.Empty;
    private string _filterMode = "all";

    private IEnumerable<BrandDto> FilteredBrands
    {
        get
        {
            var brands = Brands.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(_searchText))
                brands = brands.Where(b => b.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase));

            return _filterMode switch
            {
                "unmapped" => brands.Where(b => GetMappingCount(b.Id) < Marketplaces.Count),
                "completed" => brands.Where(b => GetMappingCount(b.Id) == Marketplaces.Count),
                _ => brands
            };
        }
    }

    private int GetMappingCount(int brandId) =>
        AllMappings.Count(m => m.ApplicationBrandId == brandId);

    private bool IsMappedTo(int brandId, int marketPlaceId) =>
        AllMappings.Any(m => m.ApplicationBrandId == brandId && m.MarketPlaceId == marketPlaceId);

    private bool IsSelected(BrandDto brand) =>
        SelectedBrand?.Id == brand.Id;

    private async Task SelectBrand(BrandDto brand)
    {
        await SelectedBrandChanged.InvokeAsync(brand);
    }

    private void OnSearchChanged(string text)
    {
        _searchText = text;
    }

    private void SetFilter(string mode)
    {
        _filterMode = mode;
    }
}

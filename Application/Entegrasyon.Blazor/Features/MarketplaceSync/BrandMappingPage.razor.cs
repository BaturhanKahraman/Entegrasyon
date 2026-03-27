using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Dtos.Marketplace;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.MarketplaceSync;

public partial class BrandMappingPage
{
    [Inject] private IBrandMatchService BrandMatchService { get; set; } = null!;
    [Inject] private IBrandService BrandService { get; set; } = null!;
    [Inject] private IMarketPlaceManager MarketPlaceManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private List<BrandDto> _brands = [];
    private List<BrandMarketPlaceMatchDto> _allMappings = [];
    private List<MarketplaceOption> _marketplaces = [];
    private BrandMappingSummaryDto _summary = new();
    private BrandDto? _selectedBrand;
    private List<BrandMarketPlaceMatchDto> _selectedBrandMappings = [];
    private bool _isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadAllData();
    }

    private async Task LoadAllData()
    {
        _isLoading = true;

        var brandsResult = await BrandService.GetBrandListDetails();
        if (brandsResult.Success && brandsResult.Data is not null)
            _brands = brandsResult.Data.Select(b => new BrandDto { Id = b.Id, Name = b.Name }).ToList();

        var mpResult = await MarketPlaceManager.GetAllAsync();
        if (mpResult.Success && mpResult.Data is not null)
            _marketplaces = mpResult.Data.Select(mp => new MarketplaceOption(mp.Id, mp.Name)).ToList();

        _summary = await BrandMatchService.GetBrandMappingsSummaryAsync();

        await LoadAllMappings();

        _isLoading = false;
    }

    private async Task LoadAllMappings()
    {
        _allMappings = [];
        foreach (var mp in _marketplaces)
        {
            var mappings = await BrandMatchService.GetAllBrandMappingsAsync(mp.Id);
            _allMappings.AddRange(mappings);
        }
    }

    private async Task OnBrandSelected(BrandDto brand)
    {
        _selectedBrand = brand;
        await LoadSelectedBrandMappings();
    }

    private async Task LoadSelectedBrandMappings()
    {
        if (_selectedBrand is null)
        {
            _selectedBrandMappings = [];
            return;
        }

        var result = await BrandMatchService.GetBrandMappingsByBrandIdAsync(_selectedBrand.Id);
        _selectedBrandMappings = result.Success && result.Data is not null ? result.Data : [];
    }

    private async Task OnMappingChanged()
    {
        await LoadAllMappings();
        await LoadSelectedBrandMappings();

        _summary = await BrandMatchService.GetBrandMappingsSummaryAsync();

        StateHasChanged();
    }
}

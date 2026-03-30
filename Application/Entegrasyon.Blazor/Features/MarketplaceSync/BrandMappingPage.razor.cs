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
    [Inject] private IBrandAutoMatchService BrandAutoMatchService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;

    private List<BrandDto> _brands = [];
    private List<BrandMarketPlaceMatchDto> _allMappings = [];
    private List<MarketplaceOption> _marketplaces = [];
    private BrandMappingSummaryDto _summary = new();
    private BrandDto? _selectedBrand;
    private List<BrandMarketPlaceMatchDto> _selectedBrandMappings = [];
    private bool _isLoading = true;
    private bool _isAutoMatching = false;
    private int? _selectedMarketplaceId;

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

    private void OnMarketplaceSelected(int? marketplaceId)
    {
        _selectedMarketplaceId = marketplaceId;
    }

    private List<BrandMarketPlaceMatchDto> GetMappingsForSelectedMarketplace()
    {
        if (_selectedMarketplaceId is null) return _allMappings;
        return _allMappings.Where(m => m.MarketPlaceId == _selectedMarketplaceId.Value).ToList();
    }

    private async Task RunAutoMatch()
    {
        if (_selectedMarketplaceId is null) return;

        _isAutoMatching = true;
        StateHasChanged();

        try
        {
            var result = await BrandAutoMatchService.AutoMatchAsync(_selectedMarketplaceId.Value);

            if (!result.Success || result.Data is null)
            {
                Snackbar.Add($"Otomatik eşleştirme başarısız: {result.Message}", Severity.Error);
                return;
            }

            await LoadAllMappings();
            _summary = await BrandMatchService.GetBrandMappingsSummaryAsync();

            var autoResult = result.Data;

            if (autoResult.Suggestions.Count > 0)
            {
                var parameters = new DialogParameters<AutoMatchResultDialog>
                {
                    { x => x.Result, autoResult }
                };
                var options = new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true, CloseButton = true };
                var dialog = await DialogService.ShowAsync<AutoMatchResultDialog>("Otomatik Eşleştirme Sonucu", parameters, options);
                var dialogResult = await dialog.Result;

                if (dialogResult is { Canceled: false, Data: List<BrandAutoMatchSuggestionDto> approvedSuggestions })
                {
                    await ApproveSuggestions(approvedSuggestions);
                }
            }
            else
            {
                var message = $"{autoResult.AutoMatchedCount} marka otomatik eşleştirildi.";
                if (autoResult.FailedCount > 0)
                    message += $" {autoResult.FailedCount} marka eşleştirilemedi.";
                Snackbar.Add(message, Severity.Success);
            }

            StateHasChanged();
        }
        finally
        {
            _isAutoMatching = false;
        }
    }

    private async Task ApproveSuggestions(List<BrandAutoMatchSuggestionDto> suggestions)
    {
        var successCount = 0;
        foreach (var suggestion in suggestions)
        {
            var dto = new CreateBrandMarketPlaceMatchDto
            {
                ApplicationBrandId = suggestion.ApplicationBrandId,
                MarketPlaceId = _selectedMarketplaceId!.Value,
                MarketPlaceBrandId = suggestion.MarketPlaceBrandId
            };
            var saveResult = await BrandMatchService.CreateBrandMappingAsync(dto);
            if (saveResult.Success) successCount++;
        }

        if (successCount > 0)
        {
            Snackbar.Add($"{successCount} öneri onaylandı ve kaydedildi.", Severity.Success);
            await LoadAllMappings();
            _summary = await BrandMatchService.GetBrandMappingsSummaryAsync();
            StateHasChanged();
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

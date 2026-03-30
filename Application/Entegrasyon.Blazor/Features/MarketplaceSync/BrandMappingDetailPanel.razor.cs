using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Dtos.Marketplace;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.MarketplaceSync;

public partial class BrandMappingDetailPanel : ComponentBase
{
    [Parameter] public BrandDto? Brand { get; set; }
    [Parameter] public List<BrandMarketPlaceMatchDto> BrandMappings { get; set; } = [];
    [Parameter] public List<MarketplaceOption> Marketplaces { get; set; } = [];
    [Parameter] public List<BrandMarketPlaceMatchDto> AllMappingsForMarketplace { get; set; } = [];
    [Parameter] public EventCallback OnMappingChanged { get; set; }

    [Inject] private IMarketplaceSearchService SearchService { get; set; } = null!;
    [Inject] private IBrandMatchService BrandMatchService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private readonly Dictionary<int, MarketplaceBrandSearchResult?> _selectedResults = new();
    private readonly HashSet<int> _savingMarketplaces = [];

    private int MappedCount => BrandMappings.Count;
    private int TotalCount => Marketplaces.Count;

    private BrandMarketPlaceMatchDto? GetMapping(int marketPlaceId) =>
        BrandMappings.FirstOrDefault(m => m.MarketPlaceId == marketPlaceId);

    /// <summary>
    /// Returns the app brand name that is already using this marketplace brand ID, or null if free.
    /// Only checks OTHER brands (not the current Brand).
    /// </summary>
    private string? GetExistingOwner(int marketPlaceId, int marketPlaceBrandId)
    {
        var existing = AllMappingsForMarketplace.FirstOrDefault(m =>
            m.MarketPlaceId == marketPlaceId &&
            m.MarketPlaceBrandId == marketPlaceBrandId &&
            m.ApplicationBrandId != (Brand?.Id ?? -1));

        return existing is not null ? existing.ApplicationBrandName : null;
    }

    private async Task<IEnumerable<MarketplaceBrandSearchResult>> SearchBrands(
        int marketPlaceId, string value, CancellationToken ct)
    {
        var result = await SearchService.SearchBrandsAsync(marketPlaceId, value ?? "", ct);
        return result.Success ? result.Data ?? [] : [];
    }

    private async Task SaveMapping(int marketPlaceId)
    {
        if (Brand is null) return;
        if (!_selectedResults.TryGetValue(marketPlaceId, out var selected) || selected is null) return;

        _savingMarketplaces.Add(marketPlaceId);
        try
        {
            var dto = new CreateBrandMarketPlaceMatchDto
            {
                ApplicationBrandId = Brand.Id,
                MarketPlaceId = marketPlaceId,
                MarketPlaceBrandId = selected.Id
            };

            var result = await BrandMatchService.CreateBrandMappingAsync(dto);
            if (result.Success)
            {
                Snackbar.Add("Eşleştirme kaydedildi.", Severity.Success);
                _selectedResults.Remove(marketPlaceId);
                await OnMappingChanged.InvokeAsync();
            }
            else
            {
                Snackbar.Add($"Hata: {result.Message}", Severity.Error);
            }
        }
        finally
        {
            _savingMarketplaces.Remove(marketPlaceId);
        }
    }

    private async Task RemoveMapping(int marketPlaceId)
    {
        if (Brand is null) return;

        var result = await BrandMatchService.RemoveBrandMappingAsync(Brand.Id, marketPlaceId);
        if (result.Success)
        {
            Snackbar.Add("Eşleştirme kaldırıldı.", Severity.Success);
            await OnMappingChanged.InvokeAsync();
        }
        else
        {
            Snackbar.Add($"Hata: {result.Message}", Severity.Error);
        }
    }

    private void OnResultSelected(int marketPlaceId, MarketplaceBrandSearchResult? value)
    {
        _selectedResults[marketPlaceId] = value;
    }
}

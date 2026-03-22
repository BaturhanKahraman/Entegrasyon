using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Dtos.Marketplace;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.MarketplaceSync;

public partial class BrandMappingDialog : ComponentBase
{
    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public BrandDto ApplicationBrand { get; set; } = null!;

    [Inject] private IMarketplaceSearchService SearchService { get; set; } = null!;
    [Inject] private IMarketPlaceManager MarketPlaceManager { get; set; } = null!;
    [Inject] private IBrandMatchService BrandMatchService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private List<MarketplaceOption> _marketplaces = [];
    private MarketplaceOption? _selectedMarketplace;
    private MarketplaceBrandSearchResult? _selectedResult;
    private bool _isLoadingMarketplaces = true;
    private bool _isSubmitting;

    protected override async Task OnInitializedAsync()
    {
        await LoadMarketplaces();
    }

    private async Task LoadMarketplaces()
    {
        _isLoadingMarketplaces = true;
        var result = await MarketPlaceManager.GetAllAsync();
        if (result.Success && result.Data is not null)
            _marketplaces = result.Data.Select(mp => new MarketplaceOption(mp.Id, mp.Name)).ToList();
        _isLoadingMarketplaces = false;
    }

    private async Task<IEnumerable<MarketplaceBrandSearchResult>> SearchMarketplaceBrands(
        string value, CancellationToken ct)
    {
        if (_selectedMarketplace is null) return [];

        var result = await SearchService.SearchBrandsAsync(_selectedMarketplace.Id, value ?? "", ct);
        return result.Success ? result.Data : [];
    }

    private async Task SubmitForm()
    {
        if (_selectedMarketplace is null || _selectedResult is null) return;

        _isSubmitting = true;
        try
        {
            var dto = new CreateBrandMarketPlaceMatchDto
            {
                ApplicationBrandId = ApplicationBrand.Id,
                MarketPlaceId = _selectedMarketplace.Id,
                MarketPlaceBrandId = _selectedResult.Id
            };

            var result = await BrandMatchService.CreateBrandMappingAsync(dto);
            if (result.Success)
            {
                Snackbar.Add("Marka eşleştirme başarıyla oluşturuldu.", Severity.Success);
                MudDialog.Close(DialogResult.Ok(dto));
            }
            else
            {
                Snackbar.Add($"Hata: {result.Message}", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Kayıt sırasında hata oluştu: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private void Cancel() => MudDialog.Cancel();
}

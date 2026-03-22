using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Dtos.Marketplace;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.MarketplaceSync;

public partial class AttributeMappingDialog : ComponentBase
{
    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public int ApplicationAttributeId { get; set; }

    [Parameter]
    public string ApplicationAttributeName { get; set; } = string.Empty;

    [Inject] private IMarketplaceSearchService SearchService { get; set; } = null!;
    [Inject] private IMarketPlaceManager MarketPlaceManager { get; set; } = null!;
    [Inject] private ICategoryAttributeManager AttributeManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private List<MarketplaceOption> _marketplaces = [];
    private MarketplaceOption? _selectedMarketplace;
    private MarketplaceAttributeSearchResult? _selectedResult;
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

    private async Task<IEnumerable<MarketplaceAttributeSearchResult>> SearchMarketplaceAttributes(
        string value, CancellationToken ct)
    {
        if (_selectedMarketplace is null) return [];

        var result = await SearchService.SearchAttributesAsync(_selectedMarketplace.Id, value ?? "", ct);
        return result.Success ? result.Data : [];
    }

    private async Task SubmitForm()
    {
        if (_selectedMarketplace is null || _selectedResult is null) return;

        _isSubmitting = true;
        try
        {
            var dto = new CreateAttributeMarketPlaceMatchDto
            {
                ApplicationCategoryAttributeId = ApplicationAttributeId,
                MarketPlaceId = _selectedMarketplace.Id,
                MarketPlaceCategoryAttributeId = _selectedResult.Id
            };

            var result = await AttributeManager.CreateAttributeMarketPlaceMatchAsync(dto);
            if (result.Success)
            {
                Snackbar.Add("Özellik eşleştirme başarıyla oluşturuldu.", Severity.Success);
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

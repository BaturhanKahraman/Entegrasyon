using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Dtos.Marketplace;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.MarketplaceSync;

public partial class CategoryMappingDialog : ComponentBase
{
    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public Category ApplicationCategory { get; set; } = null!;

    [Inject] private IMarketplaceSearchService SearchService { get; set; } = null!;
    [Inject] private IMarketPlaceManager MarketPlaceManager { get; set; } = null!;
    [Inject] private ICategoryMatchService CategoryMatchService { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private List<MarketplaceOption> _marketplaces = [];
    private MarketplaceOption? _selectedMarketplace;
    private MarketplaceCategorySearchResult? _selectedResult;
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

    private async Task<IEnumerable<MarketplaceCategorySearchResult>> SearchMarketplaceCategories(
        string value, CancellationToken ct)
    {
        if (_selectedMarketplace is null) return [];

        var result = await SearchService.SearchCategoriesAsync(_selectedMarketplace.Id, value ?? "", ct);
        return result.Success ? result.Data : [];
    }

    private async Task SubmitForm()
    {
        if (_selectedMarketplace is null || _selectedResult is null) return;

        _isSubmitting = true;
        try
        {
            var dto = new CreateCategoryMarketplaceMatchDto
            {
                ApplicationCategoryId = ApplicationCategory.Id,
                MarketPlaceId = _selectedMarketplace.Id,
                MarketPlaceCategoryId = _selectedResult.Id,
                MarketPlaceCategoryName = _selectedResult.FullPath ?? _selectedResult.Name
            };

            var result = await CategoryMatchService.CreateCategoryMappingAsync(dto);
            if (result.Success)
            {
                Snackbar.Add("Kategori eslestirme basariyla olusturuldu.", Severity.Success);
                MudDialog.Close(DialogResult.Ok(dto));

                // Ask if user wants to go to attribute mapping
                await AskForAttributeRedirect(_selectedMarketplace.Id);
            }
            else
            {
                Snackbar.Add($"Hata: {result.Message}", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Kayit sirasinda hata olustu: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private async Task AskForAttributeRedirect(int marketPlaceId)
    {
        var confirmed = await DialogService.ShowMessageBox(
            "Attribute Eslestirme",
            "Kategori eslestirmesi tamamlandi. Simdi attribute eslestirmeye gecmek ister misiniz?",
            yesText: "Attribute Eslestirmeye Git",
            cancelText: "Kapat");

        if (confirmed is true)
        {
            NavigationManager.NavigateTo(
                $"/marketplace/sync/attributes?categoryId={ApplicationCategory.Id}&marketPlaceId={marketPlaceId}");
        }
    }

    private void Cancel() => MudDialog.Cancel();
}

public record MarketplaceOption(int Id, string Name);

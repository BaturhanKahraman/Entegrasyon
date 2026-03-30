using Entegrasyon.Blazor.Features.MarketplaceSync.Templates;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using static Entegrasyon.Business.Utility.Constants.MarketPlaceConstants;

namespace Entegrasyon.Blazor.Features.MarketplaceSync;

public partial class CategorySync
{
    [Inject] private ICategoryService CategoryService { get; set; } = null!;
    [Inject] private ICategoryMatchService CategoryMatchService { get; set; } = null!;
    [Inject] private ICategoryMatchValidationService ValidationService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    private List<Category> _categories = [];
    private CategoryMatchSummaryDto _summary = new();
    private Dictionary<int, CategoryMatchValidationResultDto> _validationResults = new();
    private bool _isLoading = true;
    private bool _showUnmappedOnly;
    private bool _showLeafOnly = true;

    private IEnumerable<Category> FilteredCategories
    {
        get
        {
            IEnumerable<Category> result = _categories;

            if (_showUnmappedOnly)
                result = result.Where(c => c.MarketplaceLinks == null || !c.MarketplaceLinks.Any());

            if (_showLeafOnly)
                result = result.Where(IsLeaf);

            return result;
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadData();
    }

    private async Task LoadData()
    {
        _isLoading = true;

        _categories = await CategoryService.GetAllCategoriesWithHierarchyAsync();
        _summary = await CategoryMatchService.GetCategoryMatchSummaryAsync();
        _isLoading = false;

        // Load validation results in background (non-blocking)
        _ = LoadValidationResults();
    }

    private async Task LoadValidationResults()
    {
        var result = await ValidationService.ValidateAllMatchesAsync(TrendyolMarketPlaceId);
        if (result.Success)
        {
            _validationResults = result.Data.ToDictionary(v => v.CategoryId);
            await InvokeAsync(StateHasChanged);
        }
    }

    private CategoryMatchValidationResultDto? GetValidationStatus(int categoryId)
    {
        _validationResults.TryGetValue(categoryId, out var status);
        return status;
    }

    private async Task OpenRowMappingDialog(Category category)
    {
        var parameters = new DialogParameters<CategoryMappingDialog>
        {
            { x => x.ApplicationCategory, category }
        };
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };
        var dialog = await DialogService.ShowAsync<CategoryMappingDialog>("Kategori Eşleştirme", parameters, options);
        var result = await dialog.Result;

        if (result is not null && !result.Canceled)
            await LoadData();
    }

    private async Task DeleteMappingForMarketplace(Category category, int marketPlaceId)
    {
        var mpName = category.MarketplaceLinks?
            .FirstOrDefault(l => l.MarketPlaceId == marketPlaceId)?.MarketPlace?.Name ?? $"MP#{marketPlaceId}";

        var confirmed = await DialogService.ShowMessageBox(
            "Eşleştirmeyi Sil",
            $"'{category.Name}' kategorisinin {mpName} eşleştirmesini silmek istediğinize emin misiniz?",
            yesText: "Evet", cancelText: "İptal");

        if (confirmed is true)
        {
            var result = await CategoryMatchService.RemoveCategoryMappingAsync(category.Id, marketPlaceId);
            if (result.Success)
            {
                Snackbar.Add($"{mpName} eşleştirmesi silindi.", Severity.Success);
                await LoadData();
            }
            else
            {
                Snackbar.Add($"Hata: {result.Message}", Severity.Error);
            }
        }
    }

    private string GetParentName(Category category)
    {
        if (category.SuperCategoryId is null or <= 0)
            return "-";

        return _categories.FirstOrDefault(c => c.Id == category.SuperCategoryId)?.Name ?? "-";
    }

    private bool IsLeaf(Category category)
    {
        return !_categories.Any(c => c.SuperCategoryId == category.Id);
    }

    private void NavigateToAttributeMatch(int categoryId, int marketPlaceId)
    {
        NavigationManager.NavigateTo($"/marketplace/sync/attributes?marketplaceId={marketPlaceId}&categoryId={categoryId}");
    }

    private async Task OpenSaveTemplateDialog()
    {
        var parameters = new DialogParameters<SaveTemplateDialog>
        {
            { x => x.MarketPlaceId, TrendyolMarketPlaceId }
        };
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };
        var dialog = await DialogService.ShowAsync<SaveTemplateDialog>("Template Kaydet", parameters, options);
        await dialog.Result;
    }

    private async Task OpenApplyTemplateDialog()
    {
        var parameters = new DialogParameters<ApplyTemplateDialog>
        {
            { x => x.MarketPlaceId, TrendyolMarketPlaceId }
        };
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };
        var dialog = await DialogService.ShowAsync<ApplyTemplateDialog>("Template Uygula", parameters, options);
        var result = await dialog.Result;

        if (result is not null && !result.Canceled)
            await LoadData();
    }
}

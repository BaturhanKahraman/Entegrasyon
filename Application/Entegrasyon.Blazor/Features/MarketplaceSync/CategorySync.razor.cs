using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Categories;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.MarketplaceSync;

public partial class CategorySync
{
    [Inject] private ICategoryService CategoryService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    private List<Category> _categories = [];
    private bool _isLoading = true;
    private bool _showUnmappedOnly;
    private bool _showLeafOnly;

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
        await LoadCategories();
    }

    private async Task LoadCategories()
    {
        try
        {
            _isLoading = true;
            _categories = await CategoryService.GetAllCategoriesWithHierarchyAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Kategoriler yüklenirken hata: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isLoading = false;
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
}

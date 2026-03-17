using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels;
using Entegrasyon.Business.Channels.Events.Categories;
using Entegrasyon.Entity.Categories;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Categories;

public partial class Categories
{
    [Inject] private ICategoryService CategoryManager { get; set; } = null!;
    [Inject] private EventChannel<CategoryUpdatedEvent> CategoryEventChannel { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    private List<Category> _categories = [];
    private Category? _selectedCategory;
    private bool _loading = true;
    private string _searchString = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await LoadCategories();
    }

    private async Task LoadCategories()
    {
        _loading = true;
        _categories = await CategoryManager.GetAllCategoriesWithoutAttributesAsync();
        _loading = false;
    }

    private async Task OnCategorySelected(Category item)
    {
        // Lazy-load: detay paneli için attributes dahil yükle
        var detail = await CategoryManager.GetCategoryDetailById(item.Id);
        _selectedCategory = detail ?? item;
    }

    private async Task SelectCategory(Category category)
    {
        await OnCategorySelected(category);
    }

    private async Task OpenAddCategoryDialog()
    {
        var parameters = new DialogParameters<CategoryDialog>
        {
            { x => x.IsEditMode, false }
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            CloseButton = true
        };

        var dialog = await DialogService.ShowAsync<CategoryDialog>("Yeni Kategori Ekle", parameters, options);
        var result = await dialog.Result;

        if (!result!.Canceled)
        {
            await LoadCategories();
        }
    }

    private Task EditSelectedCategory()
    {
        if (_selectedCategory == null) return Task.CompletedTask;
        NavigationManager.NavigateTo($"/categories/edit/{_selectedCategory.Id}");
        return Task.CompletedTask;
    }

    private async Task DeleteSelectedCategory()
    {
        if (_selectedCategory == null) return;

        var hasSubCategories = _categories.Any(c => c.SuperCategoryId == _selectedCategory.Id);
        if (hasSubCategories)
        {
            Snackbar.Add("Bu kategorinin alt kategorileri var. Önce alt kategorileri silmeniz gerekir.", Severity.Warning);
            return;
        }

        var confirm = await DialogService.ShowMessageBox(
            "Uyarı",
            $"'{_selectedCategory.Name}' kategorisini silmek istediğinize emin misiniz? Bu işlem geri alınamaz.",
            yesText: "Sil", cancelText: "İptal");

        if (confirm == true)
        {
            try
            {
                var result = await CategoryManager.SoftDelete(_selectedCategory.Id);
                if (result.Success)
                {
                    Snackbar.Add(result.Message, Severity.Success);
                    await CategoryEventChannel.PublishAsync(new CategoryUpdatedEvent(_selectedCategory.Id, "Deleted"));
                    _selectedCategory = null;
                    await LoadCategories();
                }
                else
                {
                    Snackbar.Add(result.Message, Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Kategori silinirken hata: {ex.Message}", Severity.Error);
            }
        }
    }

    private string GetParentCategoryName(int parentId)
    {
        return _categories.FirstOrDefault(c => c.Id == parentId)?.Name ?? "N/A";
    }

    private int GetSubcategoryCount(int categoryId)
    {
        return _categories.Count(c => c.SuperCategoryId == categoryId);
    }

    private List<Category> GetTreeItemsForView()
    {
        return _categories.Where(c => c.SuperCategoryId == null).ToList();
    }
}

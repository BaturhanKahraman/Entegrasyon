using Entegrasyon.Entity.Categories;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Categories;

public partial class CategoryTreeItem
{
    [Parameter] public Category Category { get; set; } = null!;
    [Parameter] public List<Category>? AllCategories { get; set; }
    [Parameter] public EventCallback<Category> OnCategorySelected { get; set; }

    private bool IsExpanded { get; set; }
    private bool HasChildren => Children?.Any() == true;
    private IEnumerable<Category>? Children => AllCategories?.Where(c => c.SuperCategoryId == Category.Id);

    private async Task ToggleExpanded()
    {
        IsExpanded = !IsExpanded;
        await OnCategorySelected.InvokeAsync(Category);
        StateHasChanged();
    }

    private string GetCategoryIcon()
    {
        if (HasChildren)
            return Icons.Material.Filled.Folder;
        return Icons.Material.Filled.Category;
    }

    private Color GetCategoryIconColor()
    {
        if (Category.IsFavorite)
            return Color.Warning;
        if (Category.IsImported)
            return Color.Info;
        return HasChildren ? Color.Primary : Color.Default;
    }
}

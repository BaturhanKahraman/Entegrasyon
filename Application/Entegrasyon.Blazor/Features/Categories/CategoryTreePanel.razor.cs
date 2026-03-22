using Entegrasyon.Entity.Categories;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Categories;

public partial class CategoryTreePanel
{
    [Parameter] public List<Category>? TreeItems { get; set; }
    [Parameter] public List<Category>? AllCategories { get; set; }
    [Parameter] public bool Loading { get; set; }
    [Parameter] public EventCallback<Category> CategorySelected { get; set; }
    [Parameter] public EventCallback OnAddClicked { get; set; }
    [Parameter] public EventCallback OnImportClicked { get; set; }
    [Parameter] public EventCallback<string> FilterChanged { get; set; }

    private string _searchText = string.Empty;

    private Task OnCategorySelectedInternal(Category item)
    {
        return CategorySelected.InvokeAsync(item);
    }

    private async Task OnSearchChanged(string value)
    {
        _searchText = value;
        await FilterChanged.InvokeAsync(value);
    }

    private static string GetCategoryIcon(Category category)
    {
        return category != null
            ? Icons.Material.Filled.Folder
            : Icons.Material.Filled.Category;
    }

    private Color GetCategoryIconColor(Category category)
    {
        if (category.IsFavorite)
            return Color.Warning;
        if (category.IsImported)
            return Color.Info;
        return GetSubcategories(category).Any() ? Color.Primary : Color.Default;
    }

    private IEnumerable<Category> GetSubcategories(Category category)
    {
        return AllCategories?.Where(c => c.SuperCategoryId == category.Id) ?? Enumerable.Empty<Category>();
    }
}

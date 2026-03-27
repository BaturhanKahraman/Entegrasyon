using Entegrasyon.Entity.Categories;
using Microsoft.AspNetCore.Components;

namespace Entegrasyon.Blazor.Features.MarketplaceSync.BulkCategoryMatch;

public partial class BulkMatchCategorySelector
{
    [Parameter] public List<Category> Categories { get; set; } = [];
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public HashSet<Category> SelectedCategories { get; set; } = [];
    [Parameter] public EventCallback<HashSet<Category>> OnSelectionChanged { get; set; }

    private string _searchText = string.Empty;

    private IEnumerable<Category> FilteredCategories =>
        string.IsNullOrWhiteSpace(_searchText)
            ? Categories.AsEnumerable()
            : Categories.Where(c => c.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase));

    private async Task ToggleCategory(Category category, bool selected)
    {
        if (selected)
            SelectedCategories.Add(category);
        else
            SelectedCategories.Remove(category);

        await OnSelectionChanged.InvokeAsync(SelectedCategories);
    }

    private async Task SelectAll()
    {
        foreach (var cat in FilteredCategories)
            SelectedCategories.Add(cat);

        await OnSelectionChanged.InvokeAsync(SelectedCategories);
    }

    private async Task DeselectAll()
    {
        SelectedCategories.Clear();
        await OnSelectionChanged.InvokeAsync(SelectedCategories);
    }
}

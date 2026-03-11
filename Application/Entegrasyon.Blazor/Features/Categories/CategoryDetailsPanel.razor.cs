using Entegrasyon.Entity.Categories;
using Microsoft.AspNetCore.Components;

namespace Entegrasyon.Blazor.Features.Categories;

public partial class CategoryDetailsPanel
{
    [Parameter] public Category? SelectedCategory { get; set; }
    [Parameter] public int SubcategoryCount { get; set; }
    [Parameter] public EventCallback OnEditClicked { get; set; }
    [Parameter] public EventCallback OnDeleteClicked { get; set; }
    [Parameter] public Func<int, string>? GetParentCategoryNameFunc { get; set; }

    private string GetParentCategoryName(int parentId)
    {
        return GetParentCategoryNameFunc?.Invoke(parentId) ?? "N/A";
    }
}

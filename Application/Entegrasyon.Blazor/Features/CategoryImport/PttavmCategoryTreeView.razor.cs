using Entegrasyon.Blazor.ViewModels;
using Microsoft.AspNetCore.Components;

namespace Entegrasyon.Blazor.Features.CategoryImport;

public partial class PttavmCategoryTreeView : ComponentBase
{
    [Parameter]
    public List<CategoryTreeNode> Categories { get; set; } = [];

    [Parameter]
    public IReadOnlyCollection<CategoryTreeNode>? SelectedNodes { get; set; }

    [Parameter]
    public EventCallback<IReadOnlyCollection<CategoryTreeNode>> SelectedNodesChanged { get; set; }

    [Parameter]
    public EventCallback<CategoryTreeNode> OnNodeExpanded { get; set; }

    private string _searchText = string.Empty;

    private IEnumerable<CategoryTreeNode> FilteredCategories =>
        string.IsNullOrWhiteSpace(_searchText)
            ? Categories
            : Categories.Where(c => c.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase));

    private async Task ToggleExpand(CategoryTreeNode node)
    {
        node.IsExpanded = !node.IsExpanded;
        if (node.IsExpanded && node.Children.Count == 0 && node.CanExpand)
        {
            await OnNodeExpanded.InvokeAsync(node);
        }
    }

    private async Task SelectNode(CategoryTreeNode node)
    {
        var selected = SelectedNodes?.ToList() ?? [];
        if (selected.Contains(node))
            selected.Remove(node);
        else
            selected.Add(node);

        await SelectedNodesChanged.InvokeAsync(selected.AsReadOnly());
    }

    private bool IsSelected(CategoryTreeNode node)
        => SelectedNodes?.Contains(node) ?? false;
}

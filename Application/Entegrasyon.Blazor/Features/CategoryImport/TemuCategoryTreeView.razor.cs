using Entegrasyon.Blazor.ViewModels;
using Microsoft.AspNetCore.Components;

namespace Entegrasyon.Blazor.Features.CategoryImport;

public partial class TemuCategoryTreeView
{
    [Parameter]
    public List<CategoryTreeNode> Categories { get; set; } = [];

    [Parameter]
    public IReadOnlyCollection<CategoryTreeNode>? SelectedNodes { get; set; }

    [Parameter]
    public EventCallback<IReadOnlyCollection<CategoryTreeNode>?> SelectedNodesChanged { get; set; }

    private string _searchText = string.Empty;

    private IEnumerable<CategoryTreeNode> FilteredCategories =>
        string.IsNullOrWhiteSpace(_searchText)
            ? Categories
            : FilterRecursive(Categories, _searchText);

    private void ToggleExpand(CategoryTreeNode node)
    {
        node.IsExpanded = !node.IsExpanded;
    }

    private void SelectNode(CategoryTreeNode node)
    {
        var list = SelectedNodes?.ToList() ?? new List<CategoryTreeNode>();
        if (list.Contains(node))
            list.Remove(node);
        else
            list.Add(node);
        SelectedNodes = list;
        SelectedNodesChanged.InvokeAsync(SelectedNodes);
    }

    private bool IsSelected(CategoryTreeNode node) => SelectedNodes?.Contains(node) == true;

    /// <summary>
    /// Recursive arama — ebeveyn ve children'da arama yapar.
    /// </summary>
    private static IEnumerable<CategoryTreeNode> FilterRecursive(
        IEnumerable<CategoryTreeNode> nodes, string searchText)
    {
        foreach (var node in nodes)
        {
            if (node.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            {
                yield return node;
            }
            else if (node.Children.Any())
            {
                var matchingChildren = FilterRecursive(node.Children, searchText).ToList();
                if (matchingChildren.Any())
                {
                    yield return node;
                }
            }
        }
    }
}

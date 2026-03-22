using Entegrasyon.Blazor.ViewModels;
using Microsoft.AspNetCore.Components;

namespace Entegrasyon.Blazor.Features.CategoryImport;

public partial class PazaramaCategoryTreeView
{
    [Parameter]
    public List<CategoryTreeNode> Categories { get; set; } = [];

    [Parameter]
    public IReadOnlyCollection<CategoryTreeNode>? SelectedNodes { get; set; }

    [Parameter]
    public EventCallback<IReadOnlyCollection<CategoryTreeNode>?> SelectedNodesChanged { get; set; }

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
}

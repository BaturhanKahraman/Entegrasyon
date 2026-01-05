using Entegrasyon.Blazor.ViewModels;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Components.Import;
public partial class TrendyolCategoryTreeView
{
    [Parameter]
    public List<CategoryTreeNode> Categories { get; set; } = new();

    [Parameter]
    public IReadOnlyCollection<CategoryTreeNode>? SelectedNodes { get; set; }

    [Parameter]
    public EventCallback<IReadOnlyCollection<CategoryTreeNode>?> SelectedNodesChanged { get; set; }

    [Parameter]
    public bool Loading { get; set; }

    [Parameter]
    public string SearchQuery { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<string> SearchQueryChanged { get; set; }

    private IReadOnlyCollection<TreeItemData<CategoryTreeNode>> FilteredItems { get; set; } =
        new List<TreeItemData<CategoryTreeNode>>();

    private List<FlatTreeNode> VisibleNodes { get; set; } = new();

    private IReadOnlyCollection<CategoryTreeNode>? SelectedNodesInternal
    {
        get => SelectedNodes;
        set
        {
            if (!Equals(SelectedNodes, value))
            {
                SelectedNodesChanged.InvokeAsync(value);
            }
        }
    }

    private readonly Dictionary<string, TreeItemData<CategoryTreeNode>> treeItemCache = new();

    protected override void OnParametersSet()
    {
        UpdateFilteredItems();
    }

    private async Task OnSearchChanged()
    {
        await SearchQueryChanged.InvokeAsync(SearchQuery);
        UpdateFilteredItems();
    }

    private void UpdateFilteredItems()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            // Sadece root kategorileri göster (performans için)
            FilteredItems = Categories.Where(c => string.IsNullOrEmpty(c.ParentExternalId))
                                    .Select(c => ConvertToTreeItemData(c, includeChildren: false))
                                    .ToList();
        }
        else
        {
            // Arama için tüm kategorileri filtrele ama limit koy
            var allNodes = GetAllNodes(Categories);
            var filtered = allNodes.Where(c => c.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase))
                                  .Take(100) // Maksimum 100 sonuç
                                  .ToList();
            FilteredItems = filtered.Select(c => ConvertToTreeItemData(c, includeChildren: false))
                                   .ToList();
        }
        // Virtual tree için flat list oluştur
        UpdateVisibleNodes();
    }

    private void UpdateVisibleNodes()
    {
        VisibleNodes = new List<FlatTreeNode>();
        foreach (var root in Categories.Where(c => string.IsNullOrEmpty(c.ParentExternalId)))
        {
            AddNodeToVisibleList(root, 0);
        }
    }

    private void AddNodeToVisibleList(CategoryTreeNode node, int level)
    {
        VisibleNodes.Add(new FlatTreeNode { Node = node, Level = level, Expanded = node.IsExpanded });

        if (node.IsExpanded && node.HasChildren)
        {
            foreach (var child in node.Children)
            {
                AddNodeToVisibleList(child, level + 1);
            }
        }    }

    private IEnumerable<CategoryTreeNode> GetAllNodes(IEnumerable<CategoryTreeNode> nodes)
    {
        foreach (var node in nodes)
        {
            yield return node;
            foreach (var child in GetAllNodes(node.Children))
            {
                yield return child;
            }
        }
    }

    private TreeItemData<CategoryTreeNode> ConvertToTreeItemData(CategoryTreeNode node, bool includeChildren = true)
    {
        var cacheKey = $"{node.ExternalId}_{includeChildren}";
        if (treeItemCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var treeItem = new TreeItemData<CategoryTreeNode>
        {
            Value = node,
            Children = includeChildren ? [.. node.Children.Select(c => ConvertToTreeItemData(c, includeChildren))]
                                      : new List<TreeItemData<CategoryTreeNode>>(),
            Expandable = node.HasChildren,
            Expanded = node.IsExpanded
        };

        treeItemCache[cacheKey] = treeItem;
        return treeItem;
    }
    private async Task ToggleNode(CategoryTreeNode node)
    {
        node.IsExpanded = !node.IsExpanded;
        UpdateVisibleNodes();
        StateHasChanged();
    }

    private async Task SelectNode(CategoryTreeNode node)
    {
        var currentSelection = SelectedNodes?.ToList() ?? new List<CategoryTreeNode>();

        if (currentSelection.Contains(node))
        {
            // Üst kategori seçimi kaldırılırsa alt kategorileri de kaldır
            currentSelection.Remove(node);
            RemoveAllChildren(node, currentSelection);
        }
        else
        {
            // Üst kategori seçilirse alt kategorileri de seç
            currentSelection.Add(node);
            AddAllChildren(node, currentSelection);
        }

        await SelectedNodesChanged.InvokeAsync(currentSelection);
        UpdateVisibleNodes(); // UI'yi güncellemek için
        StateHasChanged();
    }

    private void AddAllChildren(CategoryTreeNode node, List<CategoryTreeNode> selection)
    {
        foreach (var child in GetAllChildren(node))
        {
            if (!selection.Contains(child))
            {
                selection.Add(child);
            }
        }
    }

    private void RemoveAllChildren(CategoryTreeNode node, List<CategoryTreeNode> selection)
    {
        foreach (var child in GetAllChildren(node))
        {
            selection.Remove(child);
        }
    }

    private IEnumerable<CategoryTreeNode> GetAllChildren(CategoryTreeNode node)
    {
        foreach (var child in node.Children)
        {
            yield return child;
            foreach (var grandchild in GetAllChildren(child))
            {
                yield return grandchild;
            }
        }
    }

    public bool? GetSelectionState(CategoryTreeNode node)
    {
        if (SelectedNodes == null || !SelectedNodes.Any()) return false;

        var allChildren = GetAllChildren(node).ToList();
        if (!allChildren.Any()) return SelectedNodes.Contains(node);

        var selectedChildren = allChildren.Count(c => SelectedNodes.Contains(c));
        var totalChildren = allChildren.Count;

        if (selectedChildren == 0) return SelectedNodes.Contains(node);
        if (selectedChildren == totalChildren) return true;
        return null; // indeterminate
    }

    public bool GetIndeterminateState(CategoryTreeNode node)
    {
        return GetSelectionState(node) == null;
    }}

using Entegrasyon.Blazor.ViewModels;
using Entegrasyon.Business.Concrete.Import;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.CategoryImport;

public partial class N11CategoryTreeView
{
    [Inject]
    private N11CategoryImporter N11Importer { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    [Parameter]
    public List<CategoryTreeNode> Categories { get; set; } = [];

    [Parameter]
    public IReadOnlyCollection<CategoryTreeNode>? SelectedNodes { get; set; }

    [Parameter]
    public EventCallback<IReadOnlyCollection<CategoryTreeNode>?> SelectedNodesChanged { get; set; }

    private readonly HashSet<string> _fetchedNodes = new();
    private readonly HashSet<string> _loadingNodes = new();

    private async Task OnExpandNode(CategoryTreeNode node)
    {
        if (_fetchedNodes.Contains(node.ExternalId)) return;
        if (_loadingNodes.Contains(node.ExternalId)) return;

        _loadingNodes.Add(node.ExternalId);
        StateHasChanged();

        try
        {
            var n11Id = long.Parse(node.ExternalId);
            var children = await N11Importer.GetSubCategoriesAsync(n11Id);

            node.Children = children.Select(c => new CategoryTreeNode
            {
                ExternalId = c.ExternalId,
                Name = c.Name,
                ParentExternalId = c.ParentExternalId,
                Children = new HashSet<CategoryTreeNode>()
            }).ToHashSet();

            _fetchedNodes.Add(node.ExternalId);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Hata: {ex.Message}", Severity.Error);
        }
        finally
        {
            _loadingNodes.Remove(node.ExternalId);
            StateHasChanged();
        }
    }

    private bool IsNodeLoading(string externalId) => _loadingNodes.Contains(externalId);

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

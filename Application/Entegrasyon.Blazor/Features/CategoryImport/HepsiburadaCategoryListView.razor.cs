using Entegrasyon.Blazor.ViewModels;
using Microsoft.AspNetCore.Components;

namespace Entegrasyon.Blazor.Features.CategoryImport;

public partial class HepsiburadaCategoryListView
{
    [Parameter]
    public List<CategoryTreeNode> Categories { get; set; } = [];

    [Parameter]
    public IReadOnlyCollection<CategoryTreeNode>? SelectedNodes { get; set; }

    [Parameter]
    public EventCallback<IReadOnlyCollection<CategoryTreeNode>?> SelectedNodesChanged { get; set; }

    private string _searchQuery = string.Empty;
    private string SearchQuery
    {
        get => _searchQuery;
        set
        {
            _searchQuery = value;
            StateHasChanged();
        }
    }

    private HashSet<CategoryTreeNode> SelectedItemsInternal
    {
        get => SelectedNodes?.ToHashSet() ?? [];
        set => SelectedNodesChanged.InvokeAsync(value?.ToList().AsReadOnly());
    }

    private IEnumerable<CategoryTreeNode> FilteredCategories =>
        string.IsNullOrWhiteSpace(_searchQuery)
            ? Categories.AsEnumerable()
            : Categories.Where(c =>
                c.Name.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ||
                c.ExternalId.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase))
              .AsEnumerable();
}

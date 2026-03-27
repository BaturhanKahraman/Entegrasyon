using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Marketplace;
using Microsoft.AspNetCore.Components;

namespace Entegrasyon.Blazor.Features.MarketplaceSync.BulkCategoryMatch;

public partial class BulkMatchMarketplaceMapper
{
    [Parameter] public HashSet<Category> SelectedCategories { get; set; } = [];
    [Parameter] public int MarketPlaceId { get; set; }
    [Parameter] public Dictionary<int, MarketplaceCategorySearchResult?> MappingAssignments { get; set; } = new();
    [Parameter] public EventCallback<(int CategoryId, MarketplaceCategorySearchResult? Result)> OnMappingChanged { get; set; }

    [Inject] private IMarketplaceSearchService SearchService { get; set; } = null!;

    private MarketplaceCategorySearchResult? GetAssignment(int categoryId)
    {
        MappingAssignments.TryGetValue(categoryId, out var result);
        return result;
    }

    private async Task OnAssignmentChanged(int categoryId, MarketplaceCategorySearchResult? result)
    {
        MappingAssignments[categoryId] = result;
        await OnMappingChanged.InvokeAsync((categoryId, result));
    }

    private async Task<IEnumerable<MarketplaceCategorySearchResult>> SearchCategories(string query, CancellationToken ct)
    {
        var result = await SearchService.SearchCategoriesAsync(MarketPlaceId, query ?? "", ct);
        return result.Success ? result.Data : [];
    }
}

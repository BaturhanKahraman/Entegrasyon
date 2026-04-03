using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Category;

namespace Entegrasyon.MVC.Features.MarketplaceSync.ViewModels;

public class BulkCategoryMatchVm
{
    public int SelectedMarketPlaceId { get; set; } = 1;
    public List<MarketPlace> MarketPlaces { get; set; } = [];
    public CategoryMatchSummaryDto Summary { get; set; } = new();
    public List<UnmappedCategoryItemVm> UnmappedCategories { get; set; } = [];
}

public class UnmappedCategoryItemVm
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Category;

namespace Entegrasyon.MVC.Features.MarketplaceSync.ViewModels;

public class CategorySyncVm
{
    public int SelectedMarketPlaceId { get; set; } = 1;
    public List<MarketPlace> MarketPlaces { get; set; } = [];
    public CategoryMatchSummaryDto Summary { get; set; } = new();
    public List<CategoryMarketplaceMappingDto> Mappings { get; set; } = [];
    public List<CategoryListItemVm> AllCategories { get; set; } = [];
}

public class CategoryListItemVm
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsMapped { get; set; }
    public int? MarketPlaceCategoryId { get; set; }
    public string? MarketPlaceCategoryName { get; set; }
}

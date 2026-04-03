using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Product;

namespace Entegrasyon.MVC.Features.MarketplaceSync.ViewModels;

public class ProductSyncIndexVm
{
    public int SelectedMarketPlaceId { get; set; } = 1;
    public List<MarketPlace> MarketPlaces { get; set; } = [];
    public ProductSyncSummaryDto Summary { get; set; } = new(0, 0, 0, 0, 0);
    public Pageable<ProductSyncListItemDto> Products { get; set; } = new([], 0, 20, 0);
    public string? SearchKey { get; set; }
    public string? StateFilter { get; set; }
}

public class ProductSyncTableVm
{
    public int SelectedMarketPlaceId { get; set; } = 1;
    public Pageable<ProductSyncListItemDto> Products { get; set; } = new([], 0, 20, 0);
    public string? SearchKey { get; set; }
    public string? StateFilter { get; set; }
}

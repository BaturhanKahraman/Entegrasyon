using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Brand;

namespace Entegrasyon.MVC.Features.MarketplaceSync.ViewModels;

public class BrandMappingVm
{
    public int SelectedMarketPlaceId { get; set; } = 1;
    public List<MarketPlace> MarketPlaces { get; set; } = [];
    public BrandMappingSummaryDto Summary { get; set; } = new();
    public List<BrandMarketPlaceMatchDto> Mappings { get; set; } = [];
    public List<BrandDto> UnmappedBrands { get; set; } = [];
}

public class BrandDetailVm
{
    public int BrandId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public int MarketPlaceId { get; set; }
    public bool IsMapped { get; set; }
    public BrandMarketPlaceMatchDto? Mapping { get; set; }
}

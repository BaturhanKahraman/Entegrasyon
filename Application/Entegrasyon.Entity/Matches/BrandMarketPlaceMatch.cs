using Entegrasyon.Entity.Brands;

namespace Entegrasyon.Entity.Matches;

public sealed class BrandMarketPlaceMatch
{
    public int ApplicationBrandId { get; set; }
    public Brand ApplicationBrand { get; set; } = null!;
    public int MarketPlaceId { get; set; }
    public MarketPlace MarketPlace { get; set; } = null!;
    public int MarketPlaceBrandId { get; set; }
}
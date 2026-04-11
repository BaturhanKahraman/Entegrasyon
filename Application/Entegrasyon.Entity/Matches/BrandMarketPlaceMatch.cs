using Entegrasyon.Entity.Brands;

namespace Entegrasyon.Entity.Matches;

public sealed class BrandMarketPlaceMatch
{
    public int ApplicationBrandId { get; set; }
    public Brand ApplicationBrand { get; set; } = null!;
    public int MarketPlaceId { get; set; }
    public MarketPlace MarketPlace { get; set; } = null!;
    public int MarketPlaceBrandId { get; set; }

    /// <summary>
    /// String tipinde harici marka ID'si (Pazarama gibi GUID ID kullanan marketplace'ler için).
    /// </summary>
    public string? MarketPlaceBrandExternalId { get; set; }

    public string? MarketPlaceBrandName { get; set; }
}
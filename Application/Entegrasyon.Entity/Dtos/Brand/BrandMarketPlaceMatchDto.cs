namespace Entegrasyon.Entity.Dtos.Brand;

/// <summary>
/// Mevcut Brand ve Trendyol Brand arasındaki mapping'i göstermek için kullanılır.
/// </summary>
public record BrandMarketPlaceMatchDto
{
    public int ApplicationBrandId { get; set; }
    public string ApplicationBrandName { get; set; } = null!;
    public int MarketPlaceId { get; set; }
    public int MarketPlaceBrandId { get; set; }
    public string MarketPlaceBrandName { get; set; } = null!;
}

namespace Entegrasyon.Entity.Dtos.Brand;

/// <summary>
/// Marketplace brand'ını application brand'ı ile manual olarak eşleştirmek için kullanılır.
/// </summary>
public record CreateBrandMarketPlaceMatchDto
{
    public int ApplicationBrandId { get; set; }
    public int MarketPlaceId { get; set; }
    public int MarketPlaceBrandId { get; set; }
    public string? MarketPlaceBrandName { get; set; }
}

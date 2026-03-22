using Entegrasyon.Entity.Categories;

namespace Entegrasyon.Entity.Matches;

public sealed class CategoryAttributeValueMarketPlaceMatch
{
    public int ApplicationCategoryAttributeValueId { get; set; }
    public CategoryAttributeValue ApplicationCategoryAttributeValue { get; set; } = null!;
    public int MarketPlaceId { get; set; }
    public MarketPlace MarketPlace { get; set; } = null!;
    public int MarketPlaceCategoryAttributeValueId { get; set; }

    /// <summary>
    /// String tipinde harici özellik değeri ID'si (Hepsiburada gibi string ID kullanan marketplace'ler için).
    /// </summary>
    public string? MarketPlaceCategoryAttributeValueExternalId { get; set; }
}
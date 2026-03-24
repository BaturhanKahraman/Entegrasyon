namespace Entegrasyon.Entity.Templates;

/// <summary>
/// Template attribute değerinin bir marketplace'deki karşılığı.
/// </summary>
public sealed class TemplateCategoryAttrValueMarketplaceMapping : TemplateBaseEntity
{
    public int TemplateCategoryAttributeValueDataId { get; set; }
    public TemplateCategoryAttributeValueData TemplateCategoryAttributeValueData { get; set; } = null!;

    public int MarketPlaceId { get; set; }
    public int ExternalValueId { get; set; }

    /// <summary>
    /// String tipinde harici değer ID'si (Hepsiburada gibi string ID kullanan marketplace'ler için).
    /// </summary>
    public string? ExternalValueExternalId { get; set; }
}

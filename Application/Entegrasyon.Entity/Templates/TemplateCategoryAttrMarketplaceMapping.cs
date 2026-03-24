namespace Entegrasyon.Entity.Templates;

/// <summary>
/// Template attribute'unun bir marketplace'deki karşılığı.
/// </summary>
public sealed class TemplateCategoryAttrMarketplaceMapping : TemplateBaseEntity
{
    public int TemplateCategoryAttributeDataId { get; set; }
    public TemplateCategoryAttributeData TemplateCategoryAttributeData { get; set; } = null!;

    public int MarketPlaceId { get; set; }
    public int ExternalAttributeId { get; set; }

    /// <summary>
    /// String tipinde harici özellik ID'si (Hepsiburada gibi string ID kullanan marketplace'ler için).
    /// </summary>
    public string? ExternalAttributeExternalId { get; set; }
}

namespace Entegrasyon.Entity.Templates;

/// <summary>
/// Template attribute'una ait önceden tanımlanmış değer.
/// </summary>
public sealed class TemplateCategoryAttributeValueData : TemplateBaseEntity
{
    public int TemplateCategoryAttributeDataId { get; set; }
    public TemplateCategoryAttributeData TemplateCategoryAttributeData { get; set; } = null!;

    public string ValueName { get; set; } = null!;

    // Navigation
    public ICollection<TemplateCategoryAttrValueMarketplaceMapping> MarketplaceMappings { get; set; } = [];
}

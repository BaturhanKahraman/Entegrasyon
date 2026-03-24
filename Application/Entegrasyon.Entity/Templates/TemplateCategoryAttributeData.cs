namespace Entegrasyon.Entity.Templates;

/// <summary>
/// Template kategorisine ait özellik (attribute) tanımı.
/// </summary>
public sealed class TemplateCategoryAttributeData : TemplateBaseEntity
{
    public int TemplateCategoryDataId { get; set; }
    public TemplateCategoryData TemplateCategoryData { get; set; } = null!;

    public string AttributeKey { get; set; } = null!;
    public string? AttributeHumanized { get; set; }
    public bool AllowCustom { get; set; }
    public bool IsRequired { get; set; }
    public bool IsSlicer { get; set; }
    public bool IsVarianter { get; set; }

    // Navigation
    public ICollection<TemplateCategoryAttrMarketplaceMapping> MarketplaceMappings { get; set; } = [];
    public ICollection<TemplateCategoryAttributeValueData> Values { get; set; } = [];
}

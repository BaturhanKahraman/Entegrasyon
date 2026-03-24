namespace Entegrasyon.Entity.Templates;

/// <summary>
/// Template paketindeki kategori tanımı.
/// Hiyerarşik yapıyı destekler (parent → child).
/// </summary>
public sealed class TemplateCategoryData : TemplateBaseEntity
{
    public int PackageId { get; set; }
    public MatchedEntityPackage Package { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int? ParentTemplateCategoryDataId { get; set; }
    public TemplateCategoryData? Parent { get; set; }

    public int SortOrder { get; set; }
    public decimal? DefaultVatRate { get; set; }

    // Navigation
    public ICollection<TemplateCategoryData> Children { get; set; } = [];
    public ICollection<TemplateCategoryMarketplaceMapping> MarketplaceMappings { get; set; } = [];
    public ICollection<TemplateCategoryAttributeData> Attributes { get; set; } = [];
}

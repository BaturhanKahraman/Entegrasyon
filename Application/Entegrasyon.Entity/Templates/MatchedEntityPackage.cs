namespace Entegrasyon.Entity.Templates;

/// <summary>
/// Hazır eşleştirilmiş entity paketi.
/// Admin ekibi tarafından oluşturulur, kullanıcılar import eder.
/// Bir paket tüm marketplace eşleştirmelerini cross-platform olarak içerir.
/// </summary>
public sealed class MatchedEntityPackage : TemplateBaseEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public MatchedEntityType EntityType { get; set; }
    public bool IsPublished { get; set; }
    public int Version { get; set; } = 1;

    // Navigation — EntityType'a göre sadece biri dolu olur
    public ICollection<TemplateCategoryData> Categories { get; set; } = [];
    public ICollection<TemplateBrandData> Brands { get; set; } = [];
    public ICollection<TemplateCargoCompanyData> CargoCompanies { get; set; } = [];
}

using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.Entity.Categories;

/// <summary>
/// Kategori eslestirme template'i. Mevcut eslestirme durumunu kaydeder ve
/// daha sonra baska ortamlara uygulanabilir.
/// </summary>
public class CategoryMatchTemplate : BaseEntity
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }

    public int MarketPlaceId { get; set; }

    public ICollection<CategoryMatchTemplateItem> Items { get; set; } = [];
}

/// <summary>
/// Template icindeki tek bir eslestirme kaydi.
/// </summary>
public class CategoryMatchTemplateItem : BaseEntity
{
    public int Id { get; set; }
    public int TemplateId { get; set; }
    public int ApplicationCategoryId { get; set; }
    public string ApplicationCategoryName { get; set; } = string.Empty;
    public int MarketPlaceCategoryId { get; set; }
    public string? ExternalCategoryId { get; set; }
    public string? MarketPlaceCategoryName { get; set; }

    public CategoryMatchTemplate Template { get; set; } = null!;
}

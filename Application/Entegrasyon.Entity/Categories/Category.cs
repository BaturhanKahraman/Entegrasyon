using Shared.Entity;
using Entegrasyon.Entity.Products;

namespace Entegrasyon.Entity.Categories;

public sealed class Category : BaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsImported { get; set; }

    /// <summary>
    /// Kategori import kaynağı (Trendyol, Hepsiburada, N11, Manuel vb.)
    /// </summary>
    public ImportSource ImportSource { get; set; } = ImportSource.Manual;

    /// <summary>
    /// Harici sistemdeki kategori ID'si (string olarak - farklı sistemlerde farklı formatlar olabilir)
    /// </summary>
    public string? ExternalCategoryId { get; set; }

    /// <summary>
    /// [Deprecated] Trendyol ImportId - ExternalCategoryId kullanın
    /// </summary>
    [Obsolete("Use ExternalCategoryId instead. This property is kept for backward compatibility.")]
    public int? ImportId { get; set; }

    public int? SuperCategoryId { get; set; }
    public Category SuperCategory { get; set; }
    public IEnumerable<Category> SubCategories { get; set; }
    public List<CategoryAttributeCategory> CategoryAttributes { get; set; } = new ();
    public IEnumerable<Product> Products { get; set; }

    /// <summary>
    /// Marketplace eşleşmeleri
    /// </summary>
    public ICollection<CategoryMarketplace> MarketplaceLinks { get; set; } = new List<CategoryMarketplace>();
}

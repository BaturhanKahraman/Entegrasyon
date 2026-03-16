using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Entity.Categories;

/// <summary>
/// mv_category_summary materialized view'ının EF Core entity karşılığı.
/// Kategori listeleme sayfalarında 4-tablo join + aggregation yerine tek tablo scan yapılmasını sağlar.
/// </summary>
[Keyless]
public class CategorySummaryView
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = "";
    public bool IsFavorite { get; set; }
    public int? SuperCategoryId { get; set; }
    public int ProductCount { get; set; }
    public int TotalStock { get; set; }
    public int SubCategoryCount { get; set; }
    public int AttributeCount { get; set; }
    public string? SuperCategoryName { get; set; }
}

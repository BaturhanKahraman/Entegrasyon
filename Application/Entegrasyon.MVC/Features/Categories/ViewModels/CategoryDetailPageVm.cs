using Entegrasyon.Entity.Dtos.Category;

namespace Entegrasyon.MVC.Features.Categories.ViewModels;

/// <summary>
/// Kategori detay sayfası (GET /categories/{id}) view modeli.
/// Meta (kategori bilgisi + bağlı özellikler) + performans (CategoryPerformanceDto) birleşimi.
/// </summary>
public sealed class CategoryDetailPageVm
{
    public required int CategoryId { get; init; }
    public required string CategoryName { get; init; }
    public string? SuperCategoryName { get; init; }
    public int SubCategoryCount { get; init; }
    public int ProductCount { get; init; }
    public int AttributeCount { get; init; }
    public bool IsFavorite { get; init; }
    public decimal? DefaultVatRate { get; init; }

    /// <summary>Bu kategoriye doğrudan bağlı özellikler (junction satırları).</summary>
    public IReadOnlyList<CategoryAttributeDto> Attributes { get; init; } = [];

    /// <summary>Seçili pencere için satış/performans özeti. Satışsızsa skalerler 0, listeler boş.</summary>
    public required CategoryPerformanceDto Performance { get; init; }

    /// <summary>Performans penceresi (gün). KPI başlıklarında gösterilir.</summary>
    public int DaysPast => Performance.DaysPast;

    /// <summary>Bu kategoride hiç satış var mı — empty state kararı.</summary>
    public bool HasSales => Performance.TotalOrderCount > 0 || Performance.TotalSoldQuantity > 0;

    /// <summary>Yaprak kategori mi (alt kategorisi yok). Özellik/ürün yalnız yaprakta olur.</summary>
    public bool IsLeaf => SubCategoryCount == 0;
}

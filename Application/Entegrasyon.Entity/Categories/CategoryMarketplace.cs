
namespace Entegrasyon.Entity.Categories;

/// <summary>
/// Uygulama kategorisi ile pazaryeri kategorisi arasındaki eşleşme.
/// Bir uygulama kategorisi birden fazla pazaryeri kategorisiyle eşleşebilir.
/// </summary>
public sealed class CategoryMarketplace : BaseEntity
{
    public int CategoryId { get; set; }
    public Category Category { get; set; }

    public int MarketPlaceId { get; set; }
    public MarketPlace MarketPlace { get; set; }

    /// <summary>
    /// Pazaryerindeki kategori ID'si (int olarak - çoğu marketplace int kullanır)
    /// </summary>
    public int MarketPlaceCategoryId { get; set; }

    /// <summary>
    /// Pazaryerindeki kategori ID'si (string olarak - farklı formatlar için)
    /// </summary>
    public string? ExternalCategoryId { get; set; }

    /// <summary>
    /// Pazaryerindeki kategori adı (referans için)
    /// </summary>
    public string? MarketPlaceCategoryName { get; set; }

    /// <summary>
    /// Son senkronizasyon tarihi
    /// </summary>
    public DateTimeOffset? LastSyncedAt { get; set; }

    /// <summary>
    /// Eşleşme aktif mi?
    /// </summary>
    public bool IsActive { get; set; } = true;
}

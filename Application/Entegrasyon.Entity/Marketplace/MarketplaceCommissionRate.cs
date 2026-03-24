using Entegrasyon.Entity.Categories;

namespace Entegrasyon.Entity.Marketplace;

public sealed class MarketplaceCommissionRate : BaseEntity
{
    public int Id { get; set; }
    public int MarketPlaceId { get; set; }
    public MarketPlace MarketPlace { get; set; } = null!;

    /// <summary>
    /// Komisyon kategori bazlı olabilir — null ise default oran
    /// </summary>
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }

    /// <summary>
    /// Marketplace komisyon oranı (%)
    /// </summary>
    public decimal CommissionPercent { get; set; }

    /// <summary>
    /// Hizmet bedeli oranı (%)
    /// </summary>
    public decimal? ServiceFeePercent { get; set; }

    /// <summary>
    /// Sabit işlem ücreti (TL)
    /// </summary>
    public decimal? TransactionFeeFixed { get; set; }

    /// <summary>
    /// Açıklama — "Elektronik kategorisi" gibi
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Kategori belirtilmezse kullanılacak default oran
    /// </summary>
    public bool IsDefault { get; set; }
}

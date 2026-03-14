namespace Entegrasyon.Entity;

/// <summary>
/// Hangi depoların hangi marketplace'e stok göndereceğini belirleyen junction tablosu.
/// Admin ayarlar sayfasından yönetilir — TrendyolProductMapper quantity hesaplarken bu tabloyu kullanır.
/// </summary>
public sealed class MarketPlaceWarehouse : BaseEntity
{
    public int Id { get; set; }

    public int MarketPlaceId { get; set; }
    public MarketPlace MarketPlace { get; set; } = null!;

    public int BranchOfficeId { get; set; }
    public BranchOffice BranchOffice { get; set; } = null!;
}

namespace Entegrasyon.Entity.Dtos.Product;

/// <summary>
/// Ürünler liste sayfası (/products) header KPI kartlarının aggregate verisi.
/// Tek round-trip sorgu ile hesaplanır, kısa-TTL cache'lenir.
/// "Toplam Ürün" zaten Pageable.TotalItemCount'tan geldiği için burada yer almaz.
/// </summary>
/// <param name="TotalStock">
/// Tenant'taki silinmemiş ürünlerin mevcut stok toplamı.
/// Liste tablosundaki TotalCurrentStock (SUM(CurrentStock) = SUM(FirstTotalStock - SoldQuantity))
/// ile birebir aynı kaynak.
/// </param>
/// <param name="LowStockCount">
/// Mevcut stoğu DÜŞÜK olan ürün adedi. Eşik liste tablosundaki "Düşük" badge'i ile
/// birebir: stok &gt; 0 ve stok &lt; 5 (yani 1..4).
/// </param>
/// <param name="TotalVariantCount">Tenant'taki silinmemiş ProductVariant sayısı.</param>
public readonly record struct ProductListKpiDto(
    int TotalStock,
    int LowStockCount,
    int TotalVariantCount);

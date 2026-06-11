namespace Entegrasyon.Entity.Dtos.Order;

/// <summary>
/// Sipariş Hazırlama (Picking) sayfası KPI aggregate sonucu.
/// Tümü Status="Created" (MarketplaceOrderStatus) bekleyen siparişler üzerinden hesaplanır.
/// </summary>
/// <param name="PendingItemCount">Bekleyen siparişlerin toplam ürün adedi (Σ TotalQuantity).</param>
/// <param name="TodayOrderCount">Bugün (UTC) gelen bekleyen sipariş sayısı.</param>
/// <param name="StaleOrderCount">24 saatten uzun bekleyen sipariş sayısı.</param>
public record PickingKpiDto(int PendingItemCount, int TodayOrderCount, int StaleOrderCount);

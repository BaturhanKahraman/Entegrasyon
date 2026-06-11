using Entegrasyon.Entity.Dtos.Product;

namespace Entegrasyon.Business.Abstract;

/// <summary>
/// Ürün detay sayfası (GET /products/{id}) için satış/performans aggregate'i.
/// Hot-path: 2-tablo join (OrderItems → ProductVariants) + zaman penceresi
/// (Orders.OrderDate) + GroupBy. Index'ler DB Master tarafından garanti edilir
/// (ProductVariants.ProductId explicit, Orders.OrderDate explicit, OrderItems FK'leri).
/// </summary>
public interface IProductPerformanceManager
{
    /// <summary>
    /// Verilen MainProduct'a bağlı TÜM varyantların son <paramref name="daysPast"/>
    /// günündeki satış performansını hesaplar (UTC). Satışı yoksa tüm değerler 0,
    /// listeler boş döner (asla null).
    /// </summary>
    Task<ProductPerformanceDetailDto> GetProductPerformanceAsync(
        Guid productId,
        int daysPast = 30,
        CancellationToken ct = default);
}

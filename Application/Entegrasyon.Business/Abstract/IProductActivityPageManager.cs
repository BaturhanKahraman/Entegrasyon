using Entegrasyon.Entity.Dtos.Product.Activity;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

/// <summary>
/// Ürün 360° aktivite sayfasının veri katmanı orkestratörü.
/// E-ticaret/pazaryeri feature'ı opsiyoneldir: tenant'ın paketi pazaryeri iznini içermiyorsa
/// pazaryeri durum kartları ve aktivite timeline'ı SORGULANMAZ (boş döner) — sadece fiziksel
/// mağaza akışı (sipariş + stok) çalışır. Hiçbir yerde "pazaryeri var" varsayımı yapılmaz.
/// </summary>
public interface IProductActivityPageManager
{
    /// <summary>
    /// Bu tenant'ın aktif paketi e-ticaret/pazaryeri özelliğini içeriyor mu?
    /// Controller bunu view'a (ör. ecommerceEnabled) iletir.
    /// </summary>
    Task<bool> IsEcommerceEnabledAsync();

    /// <summary>
    /// Ürünün eklendiği pazaryerlerinin durum kartları. E-ticaret kapalıysa boş liste — DB'ye gidilmez.
    /// <paramref name="ecommerceEnabled"/> verilirse (caller feature'ı zaten çözdüyse) o kullanılır;
    /// null ise manager kendi kontrol eder (bağımsız caller için defansif).
    /// </summary>
    Task<IDataResult<List<ProductMarketplaceStatusDto>>> GetMarketplaceStatusesAsync(
        Guid productId, bool? ecommerceEnabled = null);

    /// <summary>
    /// Aktivite timeline (filtre + cursor pagination). E-ticaret kapalıysa boş liste — DB'ye gidilmez.
    /// <paramref name="ecommerceEnabled"/> verilirse (caller feature'ı zaten çözdüyse) o kullanılır;
    /// null ise manager kendi kontrol eder (bağımsız caller için defansif).
    /// </summary>
    Task<IDataResult<List<ProductActivityLog>>> GetTimelineAsync(
        Guid productId, ProductActivityTimelineFilter filter, int pageSize = 20, bool? ecommerceEnabled = null);

    /// <summary>
    /// Bu ürünü içeren son siparişler. Fiziksel mağaza siparişleri de dahil — feature-gate YOK.
    /// </summary>
    Task<IDataResult<List<ProductOrderReferenceDto>>> GetOrdersAsync(Guid productId, int limit = 20);

    /// <summary>
    /// Ürünün variant'larına ait stok hareketleri. Fiziksel mağaza stoğu da dahil — feature-gate YOK.
    /// </summary>
    Task<IDataResult<List<ProductStockMovementDto>>> GetStockMovementsAsync(Guid productId, int limit = 100);
}

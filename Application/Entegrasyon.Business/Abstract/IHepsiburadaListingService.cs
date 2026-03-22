using Entegrasyon.Entity.Dtos.Hepsiburada;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

/// <summary>
/// Hepsiburada listing yönetimi servisi.
/// Fiyat/stok/kargo güncelleme, activate/deactivate, listing sorgulama.
/// Base URL: listing-external[-sit].hepsiburada.com
/// </summary>
public interface IHepsiburadaListingService
{
    /// <summary>
    /// Listing bilgilerini sorgular.
    /// </summary>
    Task<IDataResult<HepsiburadaListingResponse>> GetListingsAsync(int offset = 0, int limit = 100);

    /// <summary>
    /// Toplu fiyat güncelleme.
    /// </summary>
    Task<IResult> UpdatePricesAsync(List<HepsiburadaPriceUpdateItem> items);

    /// <summary>
    /// Toplu stok güncelleme.
    /// </summary>
    Task<IResult> UpdateStocksAsync(List<HepsiburadaStockUpdateItem> items);

    /// <summary>
    /// Toplu teslimat bilgisi güncelleme.
    /// </summary>
    Task<IResult> UpdateShippingInfoAsync(List<HepsiburadaShippingInfoUpdateItem> items);

    /// <summary>
    /// Listing'i satışa açar.
    /// </summary>
    Task<IResult> ActivateListingAsync(string hepsiburadaSku, string merchantSku);

    /// <summary>
    /// Listing'i satıştan kapatır.
    /// </summary>
    Task<IResult> DeactivateListingAsync(string hepsiburadaSku, string merchantSku);

    /// <summary>
    /// Ürüne ait stok ve fiyat bilgisini senkronize eder (internal product → HB listing).
    /// </summary>
    Task<IResult> SyncProductStockAndPriceAsync(Guid productId);
}

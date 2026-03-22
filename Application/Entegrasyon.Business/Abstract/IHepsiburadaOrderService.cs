using Entegrasyon.Entity.Dtos.Hepsiburada;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

/// <summary>
/// Hepsiburada sipariş yönetimi servisi.
/// Sipariş listeleme, paketleme, kargo, iptal, fatura işlemleri.
/// Base URL: oms-external[-sit].hepsiburada.com
/// </summary>
public interface IHepsiburadaOrderService
{
    /// <summary>
    /// Ödemesi tamamlanan siparişleri listeler.
    /// </summary>
    Task<IDataResult<List<HepsiburadaOrderDto>>> GetOrdersAsync(
        DateTimeOffset? beginDate = null, DateTimeOffset? endDate = null,
        int offset = 0, int limit = 50);

    /// <summary>
    /// Sipariş detayını getirir.
    /// </summary>
    Task<IDataResult<HepsiburadaOrderDto>> GetOrderAsync(string orderNumber);

    /// <summary>
    /// Sipariş kalemlerini paketler.
    /// </summary>
    Task<IDataResult<HepsiburadaPackageResponse>> CreatePackageAsync(HepsiburadaPackageRequest request);

    /// <summary>
    /// Sipariş kalemini satıcı tarafından iptal eder.
    /// </summary>
    Task<IResult> CancelLineItemAsync(string lineItemId, int reasonId);

    /// <summary>
    /// Sipariş kalemine fatura ekler.
    /// </summary>
    Task<IResult> AddInvoiceAsync(string lineItemId, HepsiburadaInvoiceRequest invoice);
}

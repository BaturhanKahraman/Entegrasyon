using Entegrasyon.Entity.Dtos.Hepsiburada;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

/// <summary>
/// Hepsiburada ürün yönetimi servisi.
/// Ürün publish, durum takip ve PRE_MATCHED onay işlemlerini yönetir.
/// </summary>
public interface IHepsiburadaProductService
{
    /// <summary>
    /// Ürünü Hepsiburada'ya publish eder. Validation → Map → Multipart Upload → trackingId döner.
    /// </summary>
    Task<IDataResult<string>> PublishProductAsync(Guid productId);

    /// <summary>
    /// trackingId ile ürün durumunu sorgular.
    /// </summary>
    Task<IDataResult<List<HepsiburadaProductStatusItem>>> CheckProductStatusAsync(string trackingId);

    /// <summary>
    /// PRE_MATCHED statüsündeki ürünü otomatik onaylar.
    /// </summary>
    Task<IResult> ApprovePreMatchAsync(string merchantSku);
}

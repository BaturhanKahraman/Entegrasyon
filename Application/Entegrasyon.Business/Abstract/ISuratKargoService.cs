using Entegrasyon.Business.Concrete.Kargo;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

/// <summary>
/// Surat Kargo entegrasyon servisi.
/// Gonderi olusturma, takip, iptal, barkod ve etiket islemleri.
/// Marketplace'den bagimsiz, tum Siparişler icin kullanilabilir.
/// </summary>
public interface ISuratKargoService
{
    /// <summary>
    /// Yeni gonderi olusturur ve takip numarasi doner.
    /// </summary>
    Task<IDataResult<SuratKargoShipmentResult>> CreateShipmentAsync(
        SuratKargoShipmentRequest request, CancellationToken ct = default);

    /// <summary>
    /// Tracking number ile gonderi durumunu sorgular.
    /// </summary>
    Task<IDataResult<SuratKargoTrackingResult>> QueryShipmentAsync(
        string trackingNumber, CancellationToken ct = default);

    /// <summary>
    /// Gonderiyi iptal eder.
    /// </summary>
    Task<IResult> CancelShipmentAsync(
        string trackingNumber, CancellationToken ct = default);

    /// <summary>
    /// Referans numarasi ile barkod bilgisi doner (Base64).
    /// </summary>
    Task<IDataResult<string>> GetBarcodeAsync(
        string referenceNo, CancellationToken ct = default);

    /// <summary>
    /// Tracking number ile gonderi etiketi doner (Base64).
    /// </summary>
    Task<IDataResult<string>> GetShipmentLabelAsync(
        string trackingNumber, string? labelFormat = null, CancellationToken ct = default);
}

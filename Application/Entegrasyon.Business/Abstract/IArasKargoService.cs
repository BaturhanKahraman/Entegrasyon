using Entegrasyon.Business.Concrete.Kargo;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

/// <summary>
/// Aras Kargo SOAP API entegrasyon servisi.
/// Kargo gonderme, takip, iptal ve sorgulama islemlerini yonetir.
/// </summary>
public interface IArasKargoService
{
    /// <summary>
    /// Yeni kargo gonderisi olusturur (SetOrder).
    /// </summary>
    Task<IDataResult<ArasKargoOrderResult>> CreateShipmentAsync(
        ArasKargoShipmentRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Gonderiyi iptal eder (CancelDispatch).
    /// </summary>
    Task<IResult> CancelShipmentAsync(
        string integrationCode,
        CancellationToken ct = default);

    /// <summary>
    /// Entegrasyon kodu ile gonderi durumunu sorgular.
    /// </summary>
    Task<IDataResult<ArasKargoTrackingResult>> TrackShipmentAsync(
        string integrationCode,
        CancellationToken ct = default);

    /// <summary>
    /// Kargo hareket gecmisini sorgular.
    /// </summary>
    Task<IDataResult<List<ArasKargoMovement>>> GetShipmentMovementsAsync(
        string integrationCode,
        CancellationToken ct = default);

    /// <summary>
    /// Tarih araligindaki kargolari sorgular.
    /// </summary>
    Task<IDataResult<List<ArasKargoShipmentSummary>>> GetShipmentsByDateRangeAsync(
        DateTime startDate, DateTime endDate,
        CancellationToken ct = default);

    /// <summary>
    /// Teslim edilmemis kargolari listeler.
    /// </summary>
    Task<IDataResult<List<ArasKargoShipmentSummary>>> GetUndeliveredShipmentsAsync(
        CancellationToken ct = default);
}

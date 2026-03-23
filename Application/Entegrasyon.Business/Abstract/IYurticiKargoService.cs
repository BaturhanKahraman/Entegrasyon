using Entegrasyon.Business.Concrete.Kargo;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IYurticiKargoService
{
    /// <summary>
    /// Yeni kargo olusturur.
    /// </summary>
    Task<IDataResult<YurticiCreateShipmentResponse>> CreateShipmentAsync(
        YurticiCreateShipmentRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Kargo durumunu sorgular.
    /// </summary>
    Task<IDataResult<List<YurticiShipmentInfo>>> QueryShipmentAsync(
        YurticiQueryShipmentRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Kargoyu iptal eder.
    /// </summary>
    Task<IResult> CancelShipmentAsync(
        string cargoKey,
        CancellationToken ct = default);
}

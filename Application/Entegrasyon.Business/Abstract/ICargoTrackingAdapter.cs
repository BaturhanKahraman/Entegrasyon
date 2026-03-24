using Entegrasyon.Entity.Dtos.Shipping;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface ICargoTrackingAdapter
{
    int CargoCompanyId { get; }
    Task<IDataResult<ShipmentTrackingDto>> GetTrackingInfoAsync(string trackingNumber, CancellationToken ct = default);
    Task<IDataResult<List<ShipmentStatusHistoryDto>>> GetStatusHistoryAsync(string trackingNumber, CancellationToken ct = default);
}

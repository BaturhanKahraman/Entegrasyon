using Entegrasyon.Entity.Dtos.Shipping;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IShipmentTrackingManager
{
    Task<IDataResult<ShipmentTrackingDto>> TrackShipmentAsync(TrackShipmentDto dto);
    Task<IDataResult<List<ShipmentTrackingDto>>> GetAllShipmentsAsync(ShipmentFilterDto filter);
    Task<IDataResult<List<ShipmentStatusHistoryDto>>> GetShipmentHistoryAsync(long shipmentTrackingId);
    Task<IResult> RefreshTrackingStatusAsync(long shipmentTrackingId);
    Task<IDataResult<CargoSummaryDto>> GetCargoSummaryAsync();
}

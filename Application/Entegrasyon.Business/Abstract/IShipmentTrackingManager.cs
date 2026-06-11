using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Shipping;
using Entegrasyon.Entity.Requests;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IShipmentTrackingManager
{
    Task<IDataResult<ShipmentTrackingDto>> TrackShipmentAsync(TrackShipmentDto dto);
    Task<IDataResult<Pageable<ShipmentTrackingDto>>> GetAllShipmentsAsync(ShipmentPaginatedRequest request);
    Task<IDataResult<List<ShipmentStatusHistoryDto>>> GetShipmentHistoryAsync(long shipmentTrackingId);
    Task<IResult> RefreshTrackingStatusAsync(long shipmentTrackingId);
    Task<IDataResult<CargoSummaryDto>> GetCargoSummaryAsync();

    /// <summary>
    /// Kargo liste sayfası üst KPI kartları için global snapshot (Yolda / Teslim / Sorunlu
    /// kovaları). Tek server-side GroupBy(CurrentStatus), N+1/full-load yok.
    /// </summary>
    Task<ShipmentKpiDto> GetShipmentKpisAsync(CancellationToken ct = default);
}

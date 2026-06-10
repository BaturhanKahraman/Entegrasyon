using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Kargo;
using Entegrasyon.Entity.Dtos.Shipping;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Shipping;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Shipping;

/// <summary>
/// Yurtici Kargo tracking adapter. CargoCompanyId = 17 (YKMP seed).
/// OperationCode mapping: 0=Created, 1=PickedUp, 2=InTransit, 3=OutForDelivery, 4=Delivered, 5=ReturnedToSender
/// </summary>
public class YurticiTrackingAdapter(
    IYurticiKargoService yurticiKargoService,
    ILogger<YurticiTrackingAdapter> logger) : ICargoTrackingAdapter
{
    public int CargoCompanyId => 17;

    public async Task<IDataResult<ShipmentTrackingDto>> GetTrackingInfoAsync(string trackingNumber, CancellationToken ct = default)
    {
        try
        {
            var request = new YurticiQueryShipmentRequest(
                Keys: new[] { trackingNumber },
                KeyType: 0,
                AddHistoricalData: true);

            var result = await yurticiKargoService.QueryShipmentAsync(request, ct);
            if (!result.Success)
                return new ErrorDataResult<ShipmentTrackingDto>(null!, result.Message ?? "Yurtici Kargo takip bilgisi alinamadi");

            if (result.Data == null || result.Data.Count == 0)
                return new ErrorDataResult<ShipmentTrackingDto>(null!, "Yurtici Kargo gonderi bulunamadı");

            var shipment = result.Data[0];
            var status = MapOperationCode(shipment.OperationCode);

            var dto = new ShipmentTrackingDto(
                Id: 0,
                OrderId: null,
                CargoCompanyId: CargoCompanyId,
                CargoCompanyName: "Yurtici Kargo",
                TrackingNumber: shipment.CargoKey,
                CurrentStatus: status,
                LastStatusUpdate: DateTimeOffset.UtcNow,
                EstimatedDeliveryDate: null,
                ActualDeliveryDate: status == ShipmentStatus.Delivered && shipment.DeliveryDate.HasValue
                    ? new DateTimeOffset(shipment.DeliveryDate.Value, TimeSpan.Zero)
                    : null,
                RecipientName: shipment.DeliveredTo,
                RecipientAddress: null,
                StatusHistories: new List<ShipmentStatusHistoryDto>
                {
                    new(status, shipment.OperationMessage, null, DateTimeOffset.UtcNow)
                });

            return new SuccessDataResult<ShipmentTrackingDto>(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Yurtici Kargo tracking adapter hatasi: {TrackingNumber}", trackingNumber);
            return new ErrorDataResult<ShipmentTrackingDto>(null!, $"Yurtici Kargo sorgulama hatasi: {ex.Message}");
        }
    }

    public async Task<IDataResult<List<ShipmentStatusHistoryDto>>> GetStatusHistoryAsync(string trackingNumber, CancellationToken ct = default)
    {
        try
        {
            var request = new YurticiQueryShipmentRequest(
                Keys: new[] { trackingNumber },
                KeyType: 0,
                AddHistoricalData: true);

            var result = await yurticiKargoService.QueryShipmentAsync(request, ct);
            if (!result.Success)
                return new ErrorDataResult<List<ShipmentStatusHistoryDto>>(null!, result.Message ?? "Hareket bilgisi alinamadi");

            var histories = result.Data?.Select(s => new ShipmentStatusHistoryDto(
                MapOperationCode(s.OperationCode),
                s.OperationMessage,
                null,
                s.DeliveryDate.HasValue
                    ? new DateTimeOffset(s.DeliveryDate.Value, TimeSpan.Zero)
                    : DateTimeOffset.UtcNow
            )).ToList() ?? new List<ShipmentStatusHistoryDto>();

            return new SuccessDataResult<List<ShipmentStatusHistoryDto>>(histories);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Yurtici Kargo hareket sorgulama hatasi: {TrackingNumber}", trackingNumber);
            return new ErrorDataResult<List<ShipmentStatusHistoryDto>>(null!, $"Hareket sorgulama hatasi: {ex.Message}");
        }
    }

    internal static ShipmentStatus MapOperationCode(int operationCode)
    {
        return operationCode switch
        {
            0 => ShipmentStatus.Created,
            1 => ShipmentStatus.PickedUp,
            2 => ShipmentStatus.InTransit,
            3 => ShipmentStatus.OutForDelivery,
            4 => ShipmentStatus.Delivered,
            5 => ShipmentStatus.ReturnedToSender,
            6 => ShipmentStatus.Failed,
            7 => ShipmentStatus.Cancelled,
            _ => ShipmentStatus.InTransit
        };
    }
}

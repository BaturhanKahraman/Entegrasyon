using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Kargo;
using Entegrasyon.Entity.Dtos.Shipping;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Shipping;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Shipping;

/// <summary>
/// Aras Kargo tracking adapter. CargoCompanyId = 18 (ARASMP seed).
/// </summary>
public class ArasTrackingAdapter(
    IArasKargoService arasKargoService,
    ILogger<ArasTrackingAdapter> logger) : ICargoTrackingAdapter
{
    public int CargoCompanyId => 18;

    public async Task<IDataResult<ShipmentTrackingDto>> GetTrackingInfoAsync(string trackingNumber, CancellationToken ct = default)
    {
        try
        {
            var trackResult = await arasKargoService.TrackShipmentAsync(trackingNumber, ct);
            if (!trackResult.Success)
                return new ErrorDataResult<ShipmentTrackingDto>(null!, trackResult.Message ?? "Aras Kargo takip bilgisi alinamadi");

            var movementsResult = await arasKargoService.GetShipmentMovementsAsync(trackingNumber, ct);
            var histories = new List<ShipmentStatusHistoryDto>();

            if (movementsResult.Success && movementsResult.Data != null)
            {
                histories = movementsResult.Data.Select(m => new ShipmentStatusHistoryDto(
                    MapArasStatus(m.Status),
                    m.Description,
                    m.Location,
                    ParseDate(m.Date)
                )).ToList();
            }

            var data = trackResult.Data!;
            var status = MapArasStatus(data.Status);

            DateTimeOffset? deliveryDate = null;
            if (!string.IsNullOrWhiteSpace(data.DeliveryDate))
                deliveryDate = ParseDate(data.DeliveryDate);

            var dto = new ShipmentTrackingDto(
                Id: 0,
                OrderId: null,
                CargoCompanyId: CargoCompanyId,
                CargoCompanyName: "Aras Kargo",
                TrackingNumber: trackingNumber,
                CurrentStatus: status,
                LastStatusUpdate: DateTimeOffset.UtcNow,
                EstimatedDeliveryDate: null,
                ActualDeliveryDate: status == ShipmentStatus.Delivered ? deliveryDate : null,
                RecipientName: data.ReceiverName,
                RecipientAddress: null,
                StatusHistories: histories);

            return new SuccessDataResult<ShipmentTrackingDto>(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Aras Kargo tracking adapter hatasi: {TrackingNumber}", trackingNumber);
            return new ErrorDataResult<ShipmentTrackingDto>(null!, $"Aras Kargo sorgulama hatasi: {ex.Message}");
        }
    }

    public async Task<IDataResult<List<ShipmentStatusHistoryDto>>> GetStatusHistoryAsync(string trackingNumber, CancellationToken ct = default)
    {
        try
        {
            var result = await arasKargoService.GetShipmentMovementsAsync(trackingNumber, ct);
            if (!result.Success)
                return new ErrorDataResult<List<ShipmentStatusHistoryDto>>(null!, result.Message ?? "Hareket bilgisi alinamadi");

            var histories = result.Data!.Select(m => new ShipmentStatusHistoryDto(
                MapArasStatus(m.Status),
                m.Description,
                m.Location,
                ParseDate(m.Date)
            )).ToList();

            return new SuccessDataResult<List<ShipmentStatusHistoryDto>>(histories);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Aras Kargo hareket sorgulama hatasi: {TrackingNumber}", trackingNumber);
            return new ErrorDataResult<List<ShipmentStatusHistoryDto>>(null!, $"Hareket sorgulama hatasi: {ex.Message}");
        }
    }

    internal static ShipmentStatus MapArasStatus(string status)
    {
        var normalized = status.Trim().ToLowerInvariant();
        return normalized switch
        {
            var s when s.Contains("kabul") => ShipmentStatus.PickedUp,
            var s when s.Contains("aktarma") || s.Contains("transfer") => ShipmentStatus.InTransit,
            var s when s.Contains("dagitim") => ShipmentStatus.OutForDelivery,
            var s when s.Contains("teslim") => ShipmentStatus.Delivered,
            var s when s.Contains("iade") => ShipmentStatus.ReturnedToSender,
            var s when s.Contains("iptal") => ShipmentStatus.Cancelled,
            _ => ShipmentStatus.InTransit
        };
    }

    private static DateTimeOffset ParseDate(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr))
            return DateTimeOffset.UtcNow;

        if (DateTimeOffset.TryParse(dateStr, out var result))
            return result;

        return DateTimeOffset.UtcNow;
    }
}

using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Kargo;
using Entegrasyon.Entity.Dtos.Shipping;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Shipping;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Shipping;

/// <summary>
/// Surat Kargo tracking adapter. CargoCompanyId = 13 (SURATMP seed).
/// </summary>
public class SuratTrackingAdapter(
    ISuratKargoService suratKargoService,
    ILogger<SuratTrackingAdapter> logger) : ICargoTrackingAdapter
{
    public int CargoCompanyId => 13;

    public async Task<IDataResult<ShipmentTrackingDto>> GetTrackingInfoAsync(string trackingNumber, CancellationToken ct = default)
    {
        try
        {
            var result = await suratKargoService.QueryShipmentAsync(trackingNumber, ct);
            if (!result.Success)
                return new ErrorDataResult<ShipmentTrackingDto>(null!, result.Message ?? "Surat Kargo takip bilgisi alinamadi");

            var data = result.Data!;
            var status = MapSuratStatus(data.Status);

            var histories = data.Movements?.Select(m => new ShipmentStatusHistoryDto(
                MapSuratStatus(m.Description),
                m.Description,
                m.Location,
                new DateTimeOffset(m.Date, TimeSpan.Zero)
            )).ToList() ?? new List<ShipmentStatusHistoryDto>();

            var dto = new ShipmentTrackingDto(
                Id: 0,
                OrderId: null,
                CargoCompanyId: CargoCompanyId,
                CargoCompanyName: "Surat Kargo",
                TrackingNumber: data.TrackingNumber,
                CurrentStatus: status,
                LastStatusUpdate: DateTimeOffset.UtcNow,
                EstimatedDeliveryDate: null,
                ActualDeliveryDate: status == ShipmentStatus.Delivered && data.DeliveryDate.HasValue
                    ? new DateTimeOffset(data.DeliveryDate.Value, TimeSpan.Zero)
                    : null,
                RecipientName: null,
                RecipientAddress: null,
                StatusHistories: histories);

            return new SuccessDataResult<ShipmentTrackingDto>(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Surat Kargo tracking adapter hatasi: {TrackingNumber}", trackingNumber);
            return new ErrorDataResult<ShipmentTrackingDto>(null!, $"Surat Kargo sorgulama hatasi: {ex.Message}");
        }
    }

    public async Task<IDataResult<List<ShipmentStatusHistoryDto>>> GetStatusHistoryAsync(string trackingNumber, CancellationToken ct = default)
    {
        try
        {
            var result = await suratKargoService.QueryShipmentAsync(trackingNumber, ct);
            if (!result.Success)
                return new ErrorDataResult<List<ShipmentStatusHistoryDto>>(null!, result.Message ?? "Hareket bilgisi alinamadi");

            var histories = result.Data!.Movements?.Select(m => new ShipmentStatusHistoryDto(
                MapSuratStatus(m.Description),
                m.Description,
                m.Location,
                new DateTimeOffset(m.Date, TimeSpan.Zero)
            )).ToList() ?? new List<ShipmentStatusHistoryDto>();

            return new SuccessDataResult<List<ShipmentStatusHistoryDto>>(histories);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Surat Kargo hareket sorgulama hatasi: {TrackingNumber}", trackingNumber);
            return new ErrorDataResult<List<ShipmentStatusHistoryDto>>(null!, $"Hareket sorgulama hatasi: {ex.Message}");
        }
    }

    internal static ShipmentStatus MapSuratStatus(string status)
    {
        var normalized = status.Trim().ToLowerInvariant();
        return normalized switch
        {
            var s when s.Contains("kabul") || s.Contains("olustur") => ShipmentStatus.PickedUp,
            var s when s.Contains("transfer") || s.Contains("aktarma") || s.Contains("yolda") => ShipmentStatus.InTransit,
            var s when s.Contains("dagitim") => ShipmentStatus.OutForDelivery,
            var s when s.Contains("teslim") => ShipmentStatus.Delivered,
            var s when s.Contains("iade") => ShipmentStatus.ReturnedToSender,
            var s when s.Contains("iptal") => ShipmentStatus.Cancelled,
            _ => ShipmentStatus.InTransit
        };
    }
}

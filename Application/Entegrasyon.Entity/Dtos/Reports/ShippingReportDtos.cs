namespace Entegrasyon.Entity.Dtos.Reports;

// ─────────────────────────────────────────────────────────────────────────────
// Kargo Raporu — gelişmiş analitik DTO'ları (ShipmentTracking temelli).
// Gecikme tanımı: teslim-geç (ActualDeliveryDate > EstimatedDeliveryDate) VEYA
// yolda-gecikmiş (terminal değil + EstimatedDeliveryDate < şimdi).
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Kargo firması performansı — gönderi, teslim ve gecikme oranları.</summary>
public record CargoCompanyPerformanceDto(
    int CargoCompanyId,
    string CargoCompanyName,
    int TotalShipments,
    int DeliveredCount,
    int DelayedCount,
    double DeliveryRatePercent,
    double DelayRatePercent);

/// <summary>Bölge (il) bazlı teslimat yoğunluğu.</summary>
public record RegionDensityDto(string City, int ShipmentCount, int DeliveredCount);

/// <summary>Gecikme trendinde aylık nokta — gecikmiş gönderi / toplam gönderi.</summary>
public record DelayTrendPointDto(string Month, int DelayedCount, int TotalCount);

/// <summary>Gecikmiş tek gönderi (liste + toplu müşteri bilgilendirme için).</summary>
public record DelayedShipmentDto(
    long ShipmentTrackingId,
    string TrackingNumber,
    string CargoCompanyName,
    string? RecipientName,
    string? City,
    DateOnly? EstimatedDeliveryDate,
    int DaysLate,
    string StatusText,
    bool HasCustomerEmail);

/// <summary>MUTASYON isteği — seçili gecikmiş gönderilerin müşterilerini bilgilendir.</summary>
public record NotifyDelayedShipmentsDto(IReadOnlyList<long> ShipmentTrackingIds);

namespace Entegrasyon.Entity.Dtos.Shipping;

/// <summary>
/// Kargo Takip liste sayfası (GET /shipping) üst KPI kartları için global snapshot.
/// Tablo filtresinden BAĞIMSIZ — tüm (silinmemiş) sevkiyatlar üzerinden.
/// Tek server-side GroupBy(CurrentStatus) → count; kovalara bellekte pivot (N+1/full-load yok).
///
/// Not: Mevcut <c>CargoSummaryDto</c>'dan AYRI kova semantiği var (Problem kovası =
/// Failed + ReturnedToSender; InTransit = InTransit + OutForDelivery, PickedUp HARİÇ).
/// </summary>
public sealed record ShipmentKpiDto(
    // Yolda: InTransit + OutForDelivery.
    int InTransitCount,
    // Teslim edildi: Delivered.
    int DeliveredCount,
    // Sorunlu: Failed + ReturnedToSender.
    int ProblemCount
);

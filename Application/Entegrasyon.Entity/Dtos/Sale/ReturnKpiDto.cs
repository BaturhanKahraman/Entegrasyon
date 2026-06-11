namespace Entegrasyon.Entity.Dtos.Sale;

/// <summary>
/// İade Yönetimi liste sayfası (GET /returns) üst KPI kartları için global snapshot.
/// Tablo filtresinden BAĞIMSIZ — tüm (silinmemiş) iade kayıtları üzerinden.
/// Tek GroupBy(ReturnStatus) → count + conditional sum (tenant-scoped, AsNoTracking, N+1 yok).
/// </summary>
public sealed record ReturnKpiDto(
    int TotalCount,
    int PendingCount,
    int ApprovedCount,
    // Yalnız Completed iadelerin RefundAmount toplamı (gerçekleşen iade tutarı).
    decimal CompletedRefundTotal
);

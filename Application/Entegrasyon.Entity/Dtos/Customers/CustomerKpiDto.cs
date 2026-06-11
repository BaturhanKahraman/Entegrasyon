namespace Entegrasyon.Entity.Dtos.Customers;

/// <summary>
/// Müşteriler liste sayfası (GET /customers) üst KPI kartları için global snapshot.
/// Tablo filtresinden BAĞIMSIZ — tüm (silinmemiş) müşteriler üzerinden.
/// Tek server-side GroupBy(CustomerType) → count + IsActive sayımı (N+1/full-load yok).
///
/// CustomerType TPH discriminator'ı: "Retail" = bireysel (Individual), "Corporate" = kurumsal.
/// </summary>
public sealed record CustomerKpiDto(
    int IndividualCount,   // CustomerType == "Retail"
    int CorporateCount,    // CustomerType == "Corporate"
    int ActiveCount        // IsActive == true (tüm tiplerde)
);

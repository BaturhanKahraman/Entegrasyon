namespace Entegrasyon.Entity.Dtos.Invoicing;

/// <summary>
/// /invoicing sayfasi header KPI kartlari icin hafif aggregate ozet.
/// "Toplam Fatura" Pageable.TotalItemCount'tan gelir; burada eksik 3 metrik:
/// taslak sayisi, gonderilmis sayisi ve bu ay kesilen (iptal haric) genel toplam.
/// </summary>
public sealed record EInvoiceSummaryDto(
    int DraftCount,
    int SentCount,
    decimal MonthGrandTotal);

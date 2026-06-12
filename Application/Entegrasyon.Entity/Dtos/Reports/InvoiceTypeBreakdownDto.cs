namespace Entegrasyon.Entity.Dtos.Reports;

/// <summary>Fatura türü dağılımında tek segment — adet + toplam tutar.</summary>
public record InvoiceTypeSegmentDto(int Count, decimal Total);

/// <summary>
/// Belirli dönemde fatura türü dağılımı:
///  - E-Fatura / E-Arşiv: KESİLEN (Sent/Accepted) faturalar, InvoiceType'a göre (adet + GrandTotal).
///  - Faturasız: dönem içi TAMAMLANMIŞ satışlardan faturası kesilmemiş olanlar (Sale ↔ SaleItem tutarı).
/// </summary>
public record InvoiceTypeBreakdownDto(
    InvoiceTypeSegmentDto EInvoice,
    InvoiceTypeSegmentDto EArsiv,
    InvoiceTypeSegmentDto Uninvoiced)
{
    public int TotalCount => EInvoice.Count + EArsiv.Count + Uninvoiced.Count;
    public decimal TotalAmount => EInvoice.Total + EArsiv.Total + Uninvoiced.Total;
}

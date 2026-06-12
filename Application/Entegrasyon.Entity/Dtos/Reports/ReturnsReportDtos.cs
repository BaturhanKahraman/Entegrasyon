namespace Entegrasyon.Entity.Dtos.Reports;

// ─────────────────────────────────────────────────────────────────────────────
// İade Raporu — gelişmiş analitik DTO'ları (SaleReturn temelli, cross-channel).
// Gerçekleşen iade = ReturnStatus Approved veya Completed.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Nedensel iade trendinde tek seri (bir iade nedeni) — aylara hizalı adetler.</summary>
public record ReturnReasonSeriesDto(string Reason, IReadOnlyList<int> Counts);

/// <summary>
/// Nedensel iade trendi: aylık kategoriler + her iade nedeni için ayrı seri (ApexCharts stacked bar).
/// </summary>
public record ReturnReasonTrendDto(
    IReadOnlyList<string> Months,
    IReadOnlyList<ReturnReasonSeriesDto> Series);

/// <summary>Bir ürünün iade oranı — gerçekleşen iade adedi / satılan adet.</summary>
public record ProductReturnRateDto(
    Guid ProductVariantId,
    string ProductTitle,
    string Barcode,
    int SoldQuantity,
    int ReturnedQuantity,
    double ReturnRatePercent,
    bool IsAlert);

/// <summary>
/// İade maliyet özeti: iade edilen tutar + tahmini kargo + tahmini süreç (operasyon) maliyeti.
/// Kargo/süreç birim maliyetleri tahminîdir (parametreyle gelir); gerçek kargo faturası yoksa öngörü.
/// </summary>
public record ReturnCostDto(
    int ReturnCount,
    decimal TotalRefund,
    decimal EstimatedShippingCost,
    decimal EstimatedProcessCost)
{
    public decimal TotalCost => TotalRefund + EstimatedShippingCost + EstimatedProcessCost;
    public decimal AverageCostPerReturn => ReturnCount > 0 ? Math.Round(TotalCost / ReturnCount, 2) : 0m;
}

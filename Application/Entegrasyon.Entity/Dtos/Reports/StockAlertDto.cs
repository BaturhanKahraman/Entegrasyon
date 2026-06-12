namespace Entegrasyon.Entity.Dtos.Reports;

public sealed record StockAlertDto(
    Guid ProductVariantId,
    string? Barcode,
    string ProductName,
    string VariantDisplayName,
    int CurrentStock,
    int MinimumStock,
    int DaysUntilStockout,
    int SuggestedOrderQuantity,
    StockAlertLevel AlertLevel,
    int BranchOfficeId,
    string BranchOfficeName,
    DateTime? LastStockEntryDate);

public enum StockAlertLevel
{
    Sufficient,
    Low,
    Critical
}

/// <summary>
/// KPI özet — TÜM filtrelenmiş küme üzerinden hesaplanır (sayfa değil).
/// View'daki "Kritik / Düşük / Tükendi" satırı bunları gösterir.
/// </summary>
public sealed record StockAlertSummaryDto(
    int CriticalCount,
    int LowCount,
    int OutOfStockCount,
    int TotalAlerts);

/// <summary>
/// Stok alert rapor zarfı: backend-aggregate özet + sayfalı satırlar.
/// </summary>
public sealed record StockAlertReportDto(
    StockAlertSummaryDto Summary,
    Pageable<StockAlertDto> Items);

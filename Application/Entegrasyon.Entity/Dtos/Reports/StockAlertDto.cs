namespace Entegrasyon.Entity.Dtos.Reports;

public sealed record StockAlertDto(
    Guid ProductVariantId,
    string? Barcode,
    string ProductName,
    string VariantDisplayName,
    int CurrentStock,
    int MinimumStock,
    int DaysUntilStockout,
    int SuggestedOrderQuantity);

public enum StockAlertLevel
{
    Sufficient,
    Low,
    Critical
}

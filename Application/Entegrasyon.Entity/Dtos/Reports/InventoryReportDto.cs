namespace Entegrasyon.Entity.Dtos.Reports;

public sealed record InventoryReportFilterDto(
    int? BranchOfficeId,
    StockFilter StockFilter,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    bool UnsoldOnly = false);

public enum StockFilter { All, LowStock, OutOfStock }

public sealed record InventoryReportDto(
    InventoryReportSummaryDto Summary,
    List<StockItemDto> Items);

public sealed record InventoryReportSummaryDto(
    int TotalProducts,
    int TotalStock,
    int LowStockCount,
    int OutOfStockCount,
    decimal StockValue);

public sealed record StockItemDto(
    Guid ProductVariantId,
    string ProductTitle,
    string VariantTitle,
    string? Barcode,
    int CurrentStock,
    int SoldQuantity,
    string BranchOfficeName,
    decimal StockValue,
    DateOnly? LastStockEntryDate);

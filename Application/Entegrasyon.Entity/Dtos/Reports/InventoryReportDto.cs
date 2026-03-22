namespace Entegrasyon.Entity.Dtos.Reports;

public sealed record InventoryReportFilterDto(
    int? BranchOfficeId,
    StockFilter StockFilter);

public enum StockFilter { All, LowStock, OutOfStock }

public sealed record InventoryReportDto(
    InventoryReportSummaryDto Summary,
    List<StockItemDto> Items);

public sealed record InventoryReportSummaryDto(
    int TotalProducts,
    int TotalStock,
    int LowStockCount,
    int OutOfStockCount);

public sealed record StockItemDto(
    Guid ProductVariantId,
    string ProductTitle,
    string VariantTitle,
    string? Barcode,
    int CurrentStock,
    int SoldQuantity,
    string BranchOfficeName);

namespace Entegrasyon.Business.Concrete.BulkOperations;

public record ProductImportRow(
    int RowNumber,
    string Barcode,
    string Title,
    string? StockCode,
    decimal ListPrice,
    decimal SalePrice,
    decimal CostPrice,
    decimal VatRate,
    string? CategoryName,
    string? BrandName);

public record PriceImportRow(
    int RowNumber,
    string Barcode,
    decimal ListPrice,
    decimal SalePrice,
    decimal CostPrice);

public record StockImportRow(
    int RowNumber,
    string Barcode,
    int BranchOfficeId,
    int Quantity);

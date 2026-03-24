namespace Entegrasyon.Entity.Dtos.BulkOperations;

public record BulkImportRowErrorDto(int RowNumber, string? Barcode, string ErrorMessage);

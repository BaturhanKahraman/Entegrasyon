namespace Entegrasyon.Entity.Dtos.BulkOperations;

public record ImportValidationPreviewDto(
    int TotalRows,
    int ValidRows,
    int InvalidRows,
    List<BulkImportRowErrorDto> Errors);

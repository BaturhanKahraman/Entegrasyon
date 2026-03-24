namespace Entegrasyon.Entity.Dtos.BulkOperations;

public record BulkImportResultDto(
    long OperationLogId,
    int TotalRows,
    int SuccessCount,
    int ErrorCount,
    List<BulkImportRowErrorDto> Errors);

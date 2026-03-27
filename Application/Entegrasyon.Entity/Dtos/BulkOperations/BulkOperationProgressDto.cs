namespace Entegrasyon.Entity.Dtos.BulkOperations;

public record BulkOperationProgressDto(
    string Phase,
    int Current,
    int Total,
    string Message);

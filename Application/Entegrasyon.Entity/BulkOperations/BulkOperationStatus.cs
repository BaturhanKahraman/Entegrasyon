namespace Entegrasyon.Entity.BulkOperations;

public enum BulkOperationStatus
{
    Pending = 1,
    Processing = 2,
    Completed = 3,
    CompletedWithErrors = 4,
    Failed = 5
}

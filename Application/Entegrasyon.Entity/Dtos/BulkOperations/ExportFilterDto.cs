namespace Entegrasyon.Entity.Dtos.BulkOperations;

public record ExportFilterDto(
    int? CategoryId = null,
    int? BrandId = null,
    int? BranchOfficeId = null,
    bool IncludeDeleted = false);

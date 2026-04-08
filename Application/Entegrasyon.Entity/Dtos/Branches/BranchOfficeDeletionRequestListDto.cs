namespace Entegrasyon.Entity.Dtos.Branches;

public record BranchOfficeDeletionRequestListDto(
    int Id,
    int BranchOfficeId,
    string BranchOfficeName,
    int Status,
    string StatusText,
    string RequestedByUserName,
    DateTimeOffset RequestedAt,
    bool HasStockTransfer,
    int? TransferTargetBranchOfficeId,
    string? TransferTargetBranchOfficeName,
    DateTimeOffset? ReviewedAt,
    string? RejectionReason);

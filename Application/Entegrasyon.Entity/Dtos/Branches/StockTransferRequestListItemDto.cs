namespace Entegrasyon.Entity.Dtos.Branches;

public record StockTransferRequestListItemDto(
    int Id,
    int SourceBranchOfficeId,
    string SourceBranchOfficeName,
    int TargetBranchOfficeId,
    string TargetBranchOfficeName,
    int Status,
    string StatusText,
    string RequestedByUserName,
    DateTimeOffset RequestedAt,
    int TotalItemCount,
    int TotalQuantity,
    DateTimeOffset? ReviewedAt,
    string? RejectionReason);

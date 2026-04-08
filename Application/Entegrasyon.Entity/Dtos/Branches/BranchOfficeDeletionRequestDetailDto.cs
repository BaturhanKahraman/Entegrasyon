namespace Entegrasyon.Entity.Dtos.Branches;

public record BranchOfficeDeletionRequestDetailDto(
    int Id,
    int BranchOfficeId,
    string BranchOfficeName,
    int Status,
    string StatusText,
    Guid RequestedByUserId,
    string RequestedByUserName,
    DateTimeOffset RequestedAt,
    Guid? ApprovedByUserId,
    string? ApprovedByUserName,
    DateTimeOffset? ReviewedAt,
    string? RejectionReason,
    bool HasStockTransfer,
    int? TransferTargetBranchOfficeId,
    string? TransferTargetBranchOfficeName,
    IReadOnlyList<BranchOfficeDeletionRequestItemDto> SnapshotItems,
    IReadOnlyList<BranchOfficeDeletionRequestItemDto> LiveItems);

public record BranchOfficeDeletionRequestItemDto(
    Guid ProductVariantId,
    string ProductTitle,
    string Barcode,
    int Quantity);

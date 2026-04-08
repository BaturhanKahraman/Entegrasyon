namespace Entegrasyon.Entity.Dtos.Branches;

public record StockTransferRequestDetailDto(
    int Id,
    int SourceBranchOfficeId,
    string SourceBranchOfficeName,
    int TargetBranchOfficeId,
    string TargetBranchOfficeName,
    int Status,
    string StatusText,
    Guid RequestedByUserId,
    string RequestedByUserName,
    DateTimeOffset RequestedAt,
    Guid? ApprovedByUserId,
    string? ApprovedByUserName,
    DateTimeOffset? ReviewedAt,
    string? RejectionReason,
    IReadOnlyList<StockTransferItemDetailDto> Items);

public record StockTransferItemDetailDto(
    Guid ProductVariantId,
    string ProductTitle,
    string Barcode,
    int Quantity,
    int CurrentSourceStock);

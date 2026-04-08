namespace Entegrasyon.Entity.Dtos.Branches;

/// <summary>
/// Standalone stok transfer talebi oluşturma DTO'su.
/// Source ve target farklı olmalı, Items en az bir tane içermeli, her item Quantity > 0.
/// </summary>
public record CreateStockTransferRequestDto(
    int SourceBranchOfficeId,
    int TargetBranchOfficeId,
    IReadOnlyList<TransferItemDto> Items);

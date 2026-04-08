namespace Entegrasyon.Entity.Dtos.Branches;

/// <summary>
/// Şube ofisi silme talebi açma DTO'su.
/// Source ofiste stok varsa TransferTargetBranchOfficeId zorunludur.
/// </summary>
public record RequestDeleteDto(
    int BranchOfficeId,
    int? TransferTargetBranchOfficeId = null);

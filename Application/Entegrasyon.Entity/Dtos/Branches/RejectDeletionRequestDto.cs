namespace Entegrasyon.Entity.Dtos.Branches;

/// <summary>
/// Şube silme talebi red DTO'su. Reason zorunludur.
/// </summary>
public record RejectDeletionRequestDto(
    int RequestId,
    string Reason);

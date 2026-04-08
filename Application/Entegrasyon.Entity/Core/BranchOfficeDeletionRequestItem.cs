namespace Entegrasyon.Entity;

/// <summary>
/// Şube silme talebi açıldığı andaki stok snapshot'ı.
/// SADECE UI/audit göstermek için — transactional değil.
/// Onay anında gerçek transfer live re-query ile hesaplanır, bu snapshot kullanılmaz.
/// </summary>
public sealed class BranchOfficeDeletionRequestItem
{
    public int Id { get; set; }

    public int DeletionRequestId { get; set; }
    public BranchOfficeDeletionRequest DeletionRequest { get; set; } = null!;

    public Guid ProductVariantId { get; set; }
    public int Quantity { get; set; }
}

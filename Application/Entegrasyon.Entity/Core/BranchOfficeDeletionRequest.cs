using Entegrasyon.Entity.User;

namespace Entegrasyon.Entity;

/// <summary>
/// Şube ofisi silme talebi. Lifecycle: Pending → Approved | Rejected.
/// Onay anında (ApproveAsync):
///   Stage A: source'daki canlı stok TransferTargetBranchOffice'a aktarılır (bounded retry).
///   Stage B: advisory lock altında BranchOffice soft-delete + user reassign + DeletionRequestId temizlik.
/// Invariant: BranchOffice.DeletionRequestId != null ⇔ aktif ve Pending talep var.
/// </summary>
public sealed class BranchOfficeDeletionRequest : BaseEntity
{
    public int Id { get; set; }

    public int BranchOfficeId { get; set; }
    public BranchOffice BranchOffice { get; set; } = null!;

    public Guid RequestedByUserId { get; set; }
    public ApplicationUser RequestedByUser { get; set; } = null!;

    public DateTimeOffset RequestedAt { get; set; }

    public BranchOfficeDeletionRequestStatus Status { get; set; }

    public Guid? ApprovedByUserId { get; set; }
    public ApplicationUser? ApprovedByUser { get; set; }

    public DateTimeOffset? ReviewedAt { get; set; }

    public string? RejectionReason { get; set; }

    /// <summary>
    /// Onay anında stokları buraya aktarılacak hedef ofis.
    /// Null olabilir — ancak ve ancak source'un zero-stock olduğu talepte.
    /// </summary>
    public int? TransferTargetBranchOfficeId { get; set; }
    public BranchOffice? TransferTargetBranchOffice { get; set; }

    public bool HasStockTransfer { get; set; }

    public ICollection<BranchOfficeDeletionRequestItem> Items { get; set; }
        = new List<BranchOfficeDeletionRequestItem>();
}

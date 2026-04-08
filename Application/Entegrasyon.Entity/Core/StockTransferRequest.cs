using Entegrasyon.Entity.User;

namespace Entegrasyon.Entity;

/// <summary>
/// İki ofis arasında standalone stok transfer talebi (silme iş akışından bağımsız).
/// Lifecycle: Pending → Approved | Rejected.
/// Onay anında OfficeStockManager.TransferStockAsync çağrılır (atomic).
/// Onaylandıktan sonra rollback yoktur.
/// </summary>
public sealed class StockTransferRequest : BaseEntity
{
    public int Id { get; set; }

    public int SourceBranchOfficeId { get; set; }
    public BranchOffice SourceBranchOffice { get; set; } = null!;

    public int TargetBranchOfficeId { get; set; }
    public BranchOffice TargetBranchOffice { get; set; } = null!;

    public Guid RequestedByUserId { get; set; }
    public ApplicationUser RequestedByUser { get; set; } = null!;

    public DateTimeOffset RequestedAt { get; set; }

    public StockTransferRequestStatus Status { get; set; }

    public Guid? ApprovedByUserId { get; set; }
    public ApplicationUser? ApprovedByUser { get; set; }

    public DateTimeOffset? ReviewedAt { get; set; }

    public string? RejectionReason { get; set; }

    public ICollection<StockTransferRequestItem> Items { get; set; }
        = new List<StockTransferRequestItem>();
}

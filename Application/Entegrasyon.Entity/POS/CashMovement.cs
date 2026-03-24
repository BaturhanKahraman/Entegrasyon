using Entegrasyon.Entity.User;

namespace Entegrasyon.Entity.POS;

public class CashMovement : BaseEntity
{
    public long Id { get; set; }
    public long POSSessionId { get; set; }
    public CashMovementType MovementType { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    public Guid CreatedByUserId { get; set; }

    public POSSession POSSession { get; set; } = null!;
    public ApplicationUser CreatedBy { get; set; } = null!;
}

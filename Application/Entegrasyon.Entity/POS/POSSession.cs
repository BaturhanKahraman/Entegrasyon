using Entegrasyon.Entity.User;

namespace Entegrasyon.Entity.POS;

public class POSSession : BaseEntity
{
    public long Id { get; set; }
    public int BranchOfficeId { get; set; }
    public Guid CashierId { get; set; }
    public DateTimeOffset OpenedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public decimal OpeningCash { get; set; }
    public decimal? ClosingCash { get; set; }
    public POSSessionStatus Status { get; set; }
    public string? TerminalId { get; set; }

    public BranchOffice BranchOffice { get; set; } = null!;
    public ApplicationUser Cashier { get; set; } = null!;
    public ICollection<POSTransaction> Transactions { get; set; } = new List<POSTransaction>();
    public ICollection<CashMovement> CashMovements { get; set; } = new List<CashMovement>();
}

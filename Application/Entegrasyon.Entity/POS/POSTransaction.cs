using Entegrasyon.Entity.Sales;

namespace Entegrasyon.Entity.POS;

public class POSTransaction : BaseEntity
{
    public long Id { get; set; }
    public long POSSessionId { get; set; }
    public Guid SaleId { get; set; }
    public decimal CashReceived { get; set; }
    public decimal ChangeGiven { get; set; }
    public DateTimeOffset TransactionAt { get; set; }

    public POSSession POSSession { get; set; } = null!;
    public Sale Sale { get; set; } = null!;
}

using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.Entity.Sales;

public class SalePayment : BaseEntity
{
    public long Id { get; set; }
    public Guid SaleId { get; set; }
    public Sale Sale { get; set; } = null!;
    public int PaymentMethodId { get; set; }
    public PaymentMethodDefinition PaymentMethod { get; set; } = null!;
    public decimal Amount { get; set; }
    public decimal? CashReceived { get; set; }
    public decimal? ChangeGiven { get; set; }

    [StringLength(100)]
    public string? CardAuthCode { get; set; }

    [StringLength(200)]
    public string? TransactionRef { get; set; }

    public DateTimeOffset PaidAt { get; set; }
}

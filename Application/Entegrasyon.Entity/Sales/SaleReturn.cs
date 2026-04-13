using System.ComponentModel.DataAnnotations;
using Entegrasyon.Entity.User;

namespace Entegrasyon.Entity.Sales;

public class SaleReturn : BaseEntity
{
    public long Id { get; set; }
    public Guid SaleId { get; set; }
    public Sale Sale { get; set; } = null!;
    public DateTimeOffset ReturnDate { get; set; }
    public Guid ReturnedByUserId { get; set; }
    public ApplicationUser ReturnedBy { get; set; } = null!;
    public Guid? ApprovedByUserId { get; set; }
    public ApplicationUser? ApprovedBy { get; set; }
    public ReturnStatus ReturnStatus { get; set; }

    [StringLength(500)]
    public string ReturnReason { get; set; } = "";

    public int? RefundPaymentMethodId { get; set; }
    public PaymentMethodDefinition? RefundPaymentMethod { get; set; }
    public decimal RefundAmount { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    public ICollection<SaleReturnItem> Items { get; set; } = [];
}

using System.ComponentModel.DataAnnotations;
using Entegrasyon.Entity.Orders;
using Entegrasyon.Entity.User;

namespace Entegrasyon.Entity.Sales;

public class SaleReturn : BaseEntity
{
    public long Id { get; set; }

    public Guid? SaleId { get; set; }
    public Sale? Sale { get; set; }

    public Guid? OrderId { get; set; }
    public Order? Order { get; set; }

    public ReturnSource Source { get; set; } = ReturnSource.InPerson;
    public int? MarketplaceId { get; set; }

    [StringLength(200)]
    public string? MarketplaceReturnId { get; set; }

    public DateTimeOffset ReturnDate { get; set; }

    public Guid? ReturnedByUserId { get; set; }
    public ApplicationUser? ReturnedBy { get; set; }

    public Guid? ApprovedByUserId { get; set; }
    public ApplicationUser? ApprovedBy { get; set; }

    public ReturnStatus ReturnStatus { get; set; }

    public int? ReturnReasonId { get; set; }
    public ReturnReason? ReturnReason { get; set; }

    [StringLength(500)]
    public string? CustomReason { get; set; }

    public int? RefundPaymentMethodId { get; set; }
    public PaymentMethodDefinition? RefundPaymentMethod { get; set; }
    public decimal RefundAmount { get; set; }

    public int? RestoreBranchOfficeId { get; set; }
    public BranchOffice? RestoreBranchOffice { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }
    public Guid? CompletedByUserId { get; set; }
    public ApplicationUser? CompletedBy { get; set; }

    public DateTimeOffset? CancelledAt { get; set; }
    public Guid? CancelledByUserId { get; set; }
    public ApplicationUser? CancelledBy { get; set; }

    [StringLength(500)]
    public string? CancellationReason { get; set; }

    public ICollection<SaleReturnItem> Items { get; set; } = [];
}

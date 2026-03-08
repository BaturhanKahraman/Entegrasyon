using System.ComponentModel.DataAnnotations.Schema;
using Entegrasyon.Entity.Customers;
using Shared.Entity;

namespace Entegrasyon.Entity.DiscountVouchers;

public sealed class DiscountVoucher : BaseEntity
{
    public int Id { get; set; }
    public double Percentage { get; set; }
    [Column(TypeName = "numeric(18,2)")]
    public decimal Amount { get; set; }
    public string Code { get; set; }
    public DateTimeOffset? ExpiringDate { get; set; }
    public bool IsActive { get; set; }
    public int? CustomerId { get; set; }
    public Customer Customer { get; set; }

}
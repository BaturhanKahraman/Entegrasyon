using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.Entity.Sales;

public class SaleReturnItem : BaseEntity
{
    public long Id { get; set; }
    public long SaleReturnId { get; set; }
    public SaleReturn SaleReturn { get; set; } = null!;
    public Guid SaleItemId { get; set; }
    public SaleItem SaleItem { get; set; } = null!;
    public int Quantity { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }
}

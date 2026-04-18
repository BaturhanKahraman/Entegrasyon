using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.Entity.Sales;

public sealed class DiscountReason : BaseEntity
{
    public int Id { get; set; }

    [StringLength(100)]
    public string Name { get; set; } = "";

    public bool IsActive { get; set; } = true;
}

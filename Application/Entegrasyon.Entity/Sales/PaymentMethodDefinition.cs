using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.Entity.Sales;

public class PaymentMethodDefinition : BaseEntity
{
    public int Id { get; set; }

    [StringLength(100)]
    public string Name { get; set; } = "";

    [StringLength(50)]
    public string SystemCode { get; set; } = "";

    [StringLength(50)]
    public string Icon { get; set; } = "";

    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public decimal? CommissionRate { get; set; }
    public bool RequiresAuthCode { get; set; }
    public bool RequiresCashInput { get; set; }
    public int TenantId { get; set; }
}

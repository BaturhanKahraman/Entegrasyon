using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.Entity.Sales;

public class ReturnReason : BaseEntity
{
    public int Id { get; set; }

    [StringLength(50)]
    public string Code { get; set; } = "";

    [StringLength(200)]
    public string Name { get; set; } = "";

    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public bool IsSystem { get; set; }
}

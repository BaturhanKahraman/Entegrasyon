namespace Entegrasyon.Entity.Settings;

public sealed class VatRate : BaseEntity
{
    public int Id { get; set; }
    public decimal Rate { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}

namespace Entegrasyon.Entity.Storefront;

public sealed class StorefrontSizeGuide : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Name { get; set; } = null!;
    public string? CategoryIds { get; set; }
    public string? MeasurementImageUrl { get; set; }
    public string? MeasurementInstructions { get; set; }
    public string SizeData { get; set; } = null!;
    public bool IsActive { get; set; } = true;
}

namespace Entegrasyon.Entity.Storefront;

public sealed class StorefrontNewsletter : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Email { get; set; } = null!;
    public string? Name { get; set; }
    public bool IsActive { get; set; } = true;
}

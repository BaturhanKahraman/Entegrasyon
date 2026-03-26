namespace Entegrasyon.Entity.Storefront;

public sealed class StorefrontContactMessage : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Subject { get; set; }
    public string Message { get; set; } = null!;
    public bool IsRead { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}

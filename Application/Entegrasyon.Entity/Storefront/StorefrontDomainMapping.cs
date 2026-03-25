namespace Entegrasyon.Entity.Storefront;

public sealed class StorefrontDomainMapping : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string DomainName { get; set; } = null!;
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
    public SslStatus SslStatus { get; set; }
    public DateTimeOffset? SslExpiresAt { get; set; }
}

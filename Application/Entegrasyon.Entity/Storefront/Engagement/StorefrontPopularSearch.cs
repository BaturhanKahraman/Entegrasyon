namespace Entegrasyon.Entity.Storefront;

public sealed class StorefrontPopularSearch : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Query { get; set; } = null!;
    public int SearchCount { get; set; }
    public DateTimeOffset LastSearchedAt { get; set; }
}

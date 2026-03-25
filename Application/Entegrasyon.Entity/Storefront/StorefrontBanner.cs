namespace Entegrasyon.Entity.Storefront;

public sealed class StorefrontBanner : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Title { get; set; } = null!;
    public string ImageUrl { get; set; } = null!;
    public string? MobileImageUrl { get; set; }
    public string? LinkUrl { get; set; }
    public BannerPosition Position { get; set; }
    public int DisplayOrder { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}

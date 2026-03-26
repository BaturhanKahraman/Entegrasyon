namespace Entegrasyon.Entity.Storefront;

public sealed class StorefrontPage : BaseEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Title { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string ContentHtml { get; set; } = null!;
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public bool IsPublished { get; set; }
    public int DisplayOrder { get; set; }
    public bool ShowInNavigation { get; set; }
    public bool ShowInFooter { get; set; }
}


namespace Entegrasyon.Entity.Products;

public enum MarketplaceProductStatus { Pending = 0, Published = 1, Failed = 2, Rejected = 3 }

public sealed class ProductMarketplace : BaseEntity
{
    public int Id { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int MarketPlaceId { get; set; }
    public MarketPlace MarketPlace { get; set; } = null!;
    public MarketplaceProductStatus Status { get; set; } = MarketplaceProductStatus.Pending;
    public string? BatchRequestId { get; set; }
    public string? ExternalProductId { get; set; }
    public string? StatusMessage { get; set; }
    public DateTimeOffset? LastSyncedAt { get; set; }

    /// <summary>
    /// Trendyol'un onaylı ürün güncellemesi için zorunlu tuttuğu contentId.
    /// </summary>
    public long? ContentId { get; set; }

    /// <summary>
    /// Ürünün Trendyol tarafından onaylanıp onaylanmadığı.
    /// </summary>
    public bool? IsApproved { get; set; }

    /// <summary>
    /// Ürünün Trendyol'da arşivlenip arşivlenmediği.
    /// </summary>
    public bool? IsArchived { get; set; }

    // Pazaryerine özel override alanları — null ise ürünün kendi değeri kullanılır
    public string? TitleOverride { get; set; }
    public string? DescriptionOverride { get; set; }

    // Navigation property
    public ICollection<ProductVariantMarketplaceOverride> VariantOverrides { get; set; } = [];
}

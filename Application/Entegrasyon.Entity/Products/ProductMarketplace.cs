using Shared.Entity;

namespace Entegrasyon.Entity.Products;

public enum MarketplaceProductStatus { Pending = 0, Published = 1, Failed = 2, Rejected = 3 }

public sealed class ProductMarketplace : BaseEntity
{
    public int Id { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int MarketPlaceId { get; set; }
    public Entegrasyon.Entity.MarketPlace MarketPlace { get; set; } = null!;
    public MarketplaceProductStatus Status { get; set; } = MarketplaceProductStatus.Pending;
    public string? BatchRequestId { get; set; }
    public string? ExternalProductId { get; set; }
    public string? StatusMessage { get; set; }
    public DateTimeOffset? LastSyncedAt { get; set; }
}

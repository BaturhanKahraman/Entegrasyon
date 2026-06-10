using Entegrasyon.Entity.Products;

namespace Entegrasyon.Entity.Dtos.Product.Activity;

/// <summary>
/// Ürün 360° sayfasındaki pazaryeri durum kartı için projeksiyon.
/// Sadece ürünün eklendiği (ProductMarketplace kaydı olan) pazaryerleri için üretilir.
/// </summary>
public sealed record ProductMarketplaceStatusDto(
    int MarketPlaceId,
    string MarketplaceName,
    MarketplaceProductStatus Status,
    string? StatusMessage,
    string? ExternalProductId,
    bool? IsApproved,
    DateTimeOffset? LastSyncedAt);

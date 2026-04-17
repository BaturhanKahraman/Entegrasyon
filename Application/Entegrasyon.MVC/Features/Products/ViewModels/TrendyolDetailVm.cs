using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Dtos.Product.Marketplace;

namespace Entegrasyon.MVC.Features.Products.ViewModels;

public class TrendyolDetailVm
{
    public Guid ProductId { get; set; }
    public string ProductTitle { get; set; } = "";
    public string? ReturnUrl { get; set; }
    public MarketplaceSyncItemDto SyncInfo { get; set; } = null!;
    public MarketplaceOverrideDetailDto? Overrides { get; set; }

    /// <summary>
    /// Trendyol'dan kaldırma işlemi yapılabilir mi?
    /// Trendyol kuralı: sadece onaysız veya 1+ gün arşivlenmiş ürünler silinebilir.
    /// </summary>
    public bool CanRemove => SyncInfo.IsApproved != true || SyncInfo.IsArchived == true;
}

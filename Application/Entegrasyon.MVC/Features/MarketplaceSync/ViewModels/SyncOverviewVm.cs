using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Dtos.Product;

namespace Entegrasyon.MVC.Features.MarketplaceSync.ViewModels;

public class SyncOverviewVm
{
    public List<MarketplaceSyncCardVm> MarketplaceCards { get; set; } = [];
    public CategoryMatchSummaryDto CategorySummary { get; set; } = new();
    public BrandMappingSummaryDto BrandSummary { get; set; } = new();
}

public class MarketplaceSyncCardVm
{
    public int MarketPlaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    /// <summary>API anahtarları tam yapılandırılmış mı? false ise kart disabled gösterilir.</summary>
    public bool HasCredentials { get; set; }

    public ProductSyncSummaryDto SyncSummary { get; set; } = new(0, 0, 0, 0, 0);
    public int MappedCategoryCount { get; set; }
}

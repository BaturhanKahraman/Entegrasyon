using Entegrasyon.Entity.Dtos.Dashboard;

namespace Entegrasyon.Business.Abstract;

public interface IDashboardManager
{
    /// <summary>Eski monolitik metod — geriye uyumluluk için korundu.</summary>
    [Obsolete("Bölünmüş metodları kullanın: GetStatsAsync, GetWeeklySalesAsync, GetMarketplaceStatusesAsync, GetRecentActivitiesAsync")]
    Task<DashboardDto> GetDashboardAsync(int lowStockThreshold = 5);

    Task<DashboardStatsDto> GetStatsAsync(int lowStockThreshold = 5);
    Task<List<DailySalesDto>> GetWeeklySalesAsync();
    Task<List<MarketplaceStatusDto>> GetMarketplaceStatusesAsync();
    Task<List<RecentActivityDto>> GetRecentActivitiesAsync();
}

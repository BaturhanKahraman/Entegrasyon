namespace Entegrasyon.Entity.Dtos.Dashboard;

public sealed record DashboardDto(
    DashboardStatsDto Stats,
    List<DailySalesDto> WeeklySales,
    List<MarketplaceStatusDto> MarketplaceStatuses,
    List<RecentActivityDto> RecentActivities);

public sealed record DashboardStatsDto(
    int TotalProducts,
    int TotalVariants,
    int TodaySales,
    decimal TodayRevenue,
    int PendingOrders,
    int LowStockProducts);

public sealed record DailySalesDto(DateOnly Date, decimal Revenue);

public sealed record MarketplaceStatusDto(
    int MarketPlaceId,
    string Name,
    int SyncedCount,
    int PendingCount,
    int FailedCount,
    int TotalProducts);

public sealed record RecentActivityDto(
    long Id,
    string Content,
    int LogType,
    int LogAction,
    DateTimeOffset CreatedAt);

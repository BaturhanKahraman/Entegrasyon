using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Dashboard;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.Business.Tenants;

namespace Entegrasyon.Business.Concrete;

public class DashboardManager(
    IDbContextFactory<IntegrationDbContext> dbContextFactory,
    TenantMemoryCache cache) : IDashboardManager
{
    private const string StatsQuery = """
        SELECT json_build_object(
            'total_products', (SELECT COUNT(*)::int FROM "MainProducts" WHERE NOT "IsDeleted"),
            'total_variants', (SELECT COUNT(*)::int FROM "ProductVariants" WHERE NOT "IsDeleted"),
            'today_sales', (SELECT COUNT(*)::int FROM "Sales" WHERE NOT "IsDeleted"
                AND "CreatedAt" >= CURRENT_DATE::timestamptz),
            'today_revenue', (SELECT COALESCE(SUM(si."UnitPrice" * si."Quantity"), 0)
                FROM "SaleItems" si
                INNER JOIN "Sales" s ON s."Id" = si."SaleId"
                WHERE NOT si."IsDeleted" AND NOT s."IsDeleted"
                AND s."CreatedAt" >= CURRENT_DATE::timestamptz),
            'pending_orders', (SELECT COUNT(*)::int FROM "Orders"
                WHERE NOT "IsDeleted"
                AND "MarketplaceOrderStatus" IN ('Created','Picking')),
            'low_stock_products', (SELECT COUNT(*)::int FROM mv_product_stock_summary
                WHERE "TotalStock" BETWEEN 0 AND @p0)
        )::text AS "Value"
        """;

    private const string WeeklySalesQuery = """
        SELECT json_agg(row_to_json(w))::text AS "Value"
        FROM (
            SELECT
                d::date AS sale_date,
                COALESCE(SUM(si."UnitPrice" * si."Quantity"), 0) AS revenue
            FROM generate_series(
                CURRENT_DATE - INTERVAL '6 days',
                CURRENT_DATE,
                INTERVAL '1 day'
            ) d
            LEFT JOIN "Sales" s ON s."CreatedAt"::date = d::date
                AND NOT s."IsDeleted"
            LEFT JOIN "SaleItems" si ON si."SaleId" = s."Id"
                AND NOT si."IsDeleted"
            GROUP BY d::date
            ORDER BY d::date
        ) w
        """;

    private const string MarketplaceQuery = """
        SELECT COALESCE(json_agg(row_to_json(m)), '[]')::text AS "Value"
        FROM (
            SELECT
                mp."Id" AS marketplace_id,
                mp."Name" AS name,
                COUNT(*) FILTER (WHERE pm."Status" = 1)::int AS synced_count,
                COUNT(*) FILTER (WHERE pm."Status" = 0)::int AS pending_count,
                COUNT(*) FILTER (WHERE pm."Status" IN (2, 3))::int AS failed_count,
                (SELECT COUNT(*)::int FROM "MainProducts" WHERE NOT "IsDeleted") AS total_products
            FROM "MarketPlaces" mp
            LEFT JOIN "ProductMarketplaces" pm ON pm."MarketPlaceId" = mp."Id"
                AND NOT pm."IsDeleted"
            WHERE NOT mp."IsDeleted"
            GROUP BY mp."Id", mp."Name"
        ) m
        """;

    private const string RecentActivitiesQuery = """
        SELECT COALESCE(json_agg(row_to_json(r)), '[]')::text AS "Value"
        FROM (
            SELECT "Id" AS id, "Content" AS content, "LogType" AS log_type,
                   "LogAction" AS log_action, "CreatedAt" AS created_at
            FROM "Logs"
            ORDER BY "CreatedAt" DESC
            LIMIT 10
        ) r
        """;

    #pragma warning disable CS0618 // Obsolete
    public async Task<DashboardDto> GetDashboardAsync(int lowStockThreshold = 5)
    {
        var stats = await GetStatsAsync(lowStockThreshold);
        var weekly = await GetWeeklySalesAsync();
        var marketplaces = await GetMarketplaceStatusesAsync();
        var activities = await GetRecentActivitiesAsync();
        return new DashboardDto(stats, weekly, marketplaces, activities);
    }
    #pragma warning restore CS0618

    public async Task<DashboardStatsDto> GetStatsAsync(int lowStockThreshold = 5)
    {
        var key = $"dashboard:stats:{lowStockThreshold}";
        if (cache.TryGetValue(key, out DashboardStatsDto? cached) && cached is not null)
            return cached;

        await using var ctx = await dbContextFactory.CreateDbContextAsync();
        var json = await ctx.Database
            .SqlQueryRaw<string>(StatsQuery, lowStockThreshold)
            .FirstOrDefaultAsync();

        var result = string.IsNullOrEmpty(json)
            ? new DashboardStatsDto(0, 0, 0, 0m, 0, 0)
            : ParseStats(json);

        cache.Set(key, result, TimeSpan.FromMinutes(3));
        return result;
    }

    public async Task<List<DailySalesDto>> GetWeeklySalesAsync()
    {
        const string key = "dashboard:weekly";
        if (cache.TryGetValue(key, out List<DailySalesDto>? cached) && cached is not null)
            return cached;

        await using var ctx = await dbContextFactory.CreateDbContextAsync();
        var json = await ctx.Database
            .SqlQueryRaw<string>(WeeklySalesQuery)
            .FirstOrDefaultAsync();

        var result = string.IsNullOrEmpty(json) ? [] : ParseWeeklySales(json);
        cache.Set(key, result, TimeSpan.FromMinutes(5));
        return result;
    }

    public async Task<List<MarketplaceStatusDto>> GetMarketplaceStatusesAsync()
    {
        const string key = "dashboard:marketplace";
        if (cache.TryGetValue(key, out List<MarketplaceStatusDto>? cached) && cached is not null)
            return cached;

        await using var ctx = await dbContextFactory.CreateDbContextAsync();
        var json = await ctx.Database
            .SqlQueryRaw<string>(MarketplaceQuery)
            .FirstOrDefaultAsync();

        var result = string.IsNullOrEmpty(json) ? [] : ParseMarketplaces(json);
        cache.Set(key, result, TimeSpan.FromMinutes(5));
        return result;
    }

    public async Task<List<RecentActivityDto>> GetRecentActivitiesAsync()
    {
        const string key = "dashboard:activities";
        if (cache.TryGetValue(key, out List<RecentActivityDto>? cached) && cached is not null)
            return cached;

        await using var ctx = await dbContextFactory.CreateDbContextAsync();
        var json = await ctx.Database
            .SqlQueryRaw<string>(RecentActivitiesQuery)
            .FirstOrDefaultAsync();

        var result = string.IsNullOrEmpty(json) ? [] : ParseRecentActivities(json);
        cache.Set(key, result, TimeSpan.FromMinutes(1));
        return result;
    }

    private static DashboardStatsDto ParseStats(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var el = doc.RootElement;
        return new DashboardStatsDto(
            TotalProducts: el.GetProperty("total_products").GetInt32(),
            TotalVariants: el.GetProperty("total_variants").GetInt32(),
            TodaySales: el.GetProperty("today_sales").GetInt32(),
            TodayRevenue: el.GetProperty("today_revenue").GetDecimal(),
            PendingOrders: el.GetProperty("pending_orders").GetInt32(),
            LowStockProducts: el.GetProperty("low_stock_products").GetInt32()
        );
    }

    private static List<DailySalesDto> ParseWeeklySales(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var list = new List<DailySalesDto>();
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            var dateStr = item.GetProperty("sale_date").GetString()!;
            var date = DateOnly.Parse(dateStr);
            var revenue = item.GetProperty("revenue").GetDecimal();
            list.Add(new DailySalesDto(date, revenue));
        }
        return list;
    }

    private static List<MarketplaceStatusDto> ParseMarketplaces(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var list = new List<MarketplaceStatusDto>();
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            list.Add(new MarketplaceStatusDto(
                MarketPlaceId: item.GetProperty("marketplace_id").GetInt32(),
                Name: item.GetProperty("name").GetString() ?? "",
                SyncedCount: item.GetProperty("synced_count").GetInt32(),
                PendingCount: item.GetProperty("pending_count").GetInt32(),
                FailedCount: item.GetProperty("failed_count").GetInt32(),
                TotalProducts: item.GetProperty("total_products").GetInt32()
            ));
        }
        return list;
    }

    private static List<RecentActivityDto> ParseRecentActivities(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var list = new List<RecentActivityDto>();
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            list.Add(new RecentActivityDto(
                Id: item.GetProperty("id").GetInt64(),
                Content: item.GetProperty("content").GetString() ?? "",
                LogType: item.GetProperty("log_type").GetInt32(),
                LogAction: item.GetProperty("log_action").GetInt32(),
                CreatedAt: item.GetProperty("created_at").GetDateTimeOffset()
            ));
        }
        return list;
    }
}

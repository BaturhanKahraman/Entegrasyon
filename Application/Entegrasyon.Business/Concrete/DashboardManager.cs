using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete;

public class DashboardManager(IDbContextFactory<IntegrationDbContext> dbContextFactory) : IDashboardManager
{
    private const string DashboardQuery = """
        WITH stats AS (
            SELECT
                (SELECT COUNT(*)::int FROM "MainProducts" WHERE NOT "IsDeleted") AS total_products,
                (SELECT COUNT(*)::int FROM "ProductVariants" WHERE NOT "IsDeleted") AS total_variants,
                (SELECT COUNT(*)::int FROM "Sales" WHERE NOT "IsDeleted"
                 AND "CreatedAt" >= CURRENT_DATE::timestamptz) AS today_sales,
                (SELECT COALESCE(SUM(si."UnitPrice" * si."Quantity"), 0)
                 FROM "SaleItems" si
                 INNER JOIN "Sales" s ON s."Id" = si."SaleId"
                 WHERE NOT si."IsDeleted" AND NOT s."IsDeleted"
                 AND s."CreatedAt" >= CURRENT_DATE::timestamptz) AS today_revenue,
                (SELECT COUNT(*)::int FROM "Orders"
                 WHERE NOT "IsDeleted"
                 AND "MarketplaceOrderStatus" IN ('Created','Picking')) AS pending_orders,
                (SELECT COUNT(*)::int FROM (
                    SELECT pv."ProductId"
                    FROM "ProductVariants" pv
                    LEFT JOIN "BranchOfficeStocks" bos ON bos."ProductVariantId" = pv."Id"
                    WHERE NOT pv."IsDeleted"
                    GROUP BY pv."ProductId"
                    HAVING COALESCE(SUM(bos."CurrentStock"), 0) BETWEEN 0 AND @p0
                ) sub) AS low_stock_products
        ),
        weekly AS (
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
        ),
        marketplace_sync AS (
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
        ),
        recent_logs AS (
            SELECT "Id" AS id, "Content" AS content, "LogType" AS log_type, "LogAction" AS log_action, "CreatedAt" AS created_at
            FROM "Logs"
            ORDER BY "CreatedAt" DESC
            LIMIT 10
        )
        SELECT json_build_object(
            'stats', (SELECT row_to_json(stats) FROM stats),
            'weekly', (SELECT COALESCE(json_agg(row_to_json(weekly)), '[]') FROM weekly),
            'marketplaces', (SELECT COALESCE(json_agg(row_to_json(marketplace_sync)), '[]') FROM marketplace_sync),
            'recentLogs', (SELECT COALESCE(json_agg(row_to_json(recent_logs)), '[]') FROM recent_logs)
        )::text AS "Value"
        """;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public async Task<DashboardDto> GetDashboardAsync(int lowStockThreshold = 5)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var jsonResult = await dbContext.Database
            .SqlQueryRaw<string>(DashboardQuery, lowStockThreshold)
            .FirstOrDefaultAsync();

        if (string.IsNullOrEmpty(jsonResult))
            return EmptyDashboard();

        return ParseDashboardJson(jsonResult);
    }

    private static DashboardDto ParseDashboardJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var stats = ParseStats(root.GetProperty("stats"));
        var weekly = ParseWeeklySales(root.GetProperty("weekly"));
        var marketplaces = ParseMarketplaces(root.GetProperty("marketplaces"));
        var recentLogs = ParseRecentActivities(root.GetProperty("recentLogs"));

        return new DashboardDto(stats, weekly, marketplaces, recentLogs);
    }

    private static DashboardStatsDto ParseStats(JsonElement el) => new(
        TotalProducts: el.GetProperty("total_products").GetInt32(),
        TotalVariants: el.GetProperty("total_variants").GetInt32(),
        TodaySales: el.GetProperty("today_sales").GetInt32(),
        TodayRevenue: el.GetProperty("today_revenue").GetDecimal(),
        PendingOrders: el.GetProperty("pending_orders").GetInt32(),
        LowStockProducts: el.GetProperty("low_stock_products").GetInt32()
    );

    private static List<DailySalesDto> ParseWeeklySales(JsonElement el)
    {
        var list = new List<DailySalesDto>();
        foreach (var item in el.EnumerateArray())
        {
            var dateStr = item.GetProperty("sale_date").GetString()!;
            var date = DateOnly.Parse(dateStr);
            var revenue = item.GetProperty("revenue").GetDecimal();
            list.Add(new DailySalesDto(date, revenue));
        }
        return list;
    }

    private static List<MarketplaceStatusDto> ParseMarketplaces(JsonElement el)
    {
        var list = new List<MarketplaceStatusDto>();
        foreach (var item in el.EnumerateArray())
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

    private static List<RecentActivityDto> ParseRecentActivities(JsonElement el)
    {
        var list = new List<RecentActivityDto>();
        foreach (var item in el.EnumerateArray())
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

    private static DashboardDto EmptyDashboard() => new(
        new DashboardStatsDto(0, 0, 0, 0m, 0, 0),
        [], [], []);
}

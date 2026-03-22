using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Reports;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete;

public sealed class ReportManager(IDbContextFactory<IntegrationDbContext> dbContextFactory) : IReportManager
{
    #region Sales Report

    private const string SalesReportQuery = """
        WITH date_range AS (
            SELECT generate_series(@p0::date, @p1::date, '1 day'::interval)::date AS d
        ),
        sales_data AS (
            SELECT
                s."CreatedAt"::date AS sale_date,
                COUNT(DISTINCT s."Id")::int AS sale_count,
                COALESCE(SUM(si."UnitPrice" * si."Quantity"), 0) AS revenue,
                COALESCE(SUM(si."Quantity"), 0)::int AS items_sold
            FROM "Sales" s
            INNER JOIN "SaleItems" si ON si."SaleId" = s."Id" AND NOT si."IsDeleted"
            WHERE NOT s."IsDeleted"
              AND s."CreatedAt" >= @p0::timestamptz
              AND s."CreatedAt" < (@p1::date + 1)::timestamptz
            GROUP BY s."CreatedAt"::date
        ),
        daily AS (
            SELECT
                dr.d AS sale_date,
                COALESCE(sd.sale_count, 0) AS sale_count,
                COALESCE(sd.revenue, 0) AS revenue
            FROM date_range dr
            LEFT JOIN sales_data sd ON sd.sale_date = dr.d
            ORDER BY dr.d
        ),
        tax_data AS (
            SELECT
                COALESCE(SUM(
                    si."UnitPrice" * si."Quantity" * (1 - si."DiscountPercent" / 100.0)
                    * si."TaxPercentage" / 100.0
                ), 0) AS total_tax
            FROM "SaleItems" si
            INNER JOIN "Sales" s ON s."Id" = si."SaleId" AND NOT s."IsDeleted"
            WHERE NOT si."IsDeleted"
              AND s."CreatedAt" >= @p0::timestamptz
              AND s."CreatedAt" < (@p1::date + 1)::timestamptz
        ),
        summary AS (
            SELECT
                COALESCE(SUM(sale_count), 0)::int AS total_sales,
                COALESCE(SUM(revenue), 0) AS total_revenue,
                COALESCE(SUM(sd.items_sold), 0)::int AS total_items,
                (SELECT total_tax FROM tax_data) AS total_tax
            FROM sales_data sd
        ),
        top_products AS (
            SELECT
                p."Title" AS product_title,
                COALESCE(pv."Barcode", pv."Id"::text) AS variant_title,
                SUM(si."Quantity")::int AS quantity_sold,
                SUM(si."UnitPrice" * si."Quantity") AS revenue
            FROM "SaleItems" si
            INNER JOIN "Sales" s ON s."Id" = si."SaleId" AND NOT s."IsDeleted"
            INNER JOIN "ProductVariants" pv ON pv."Id" = si."ProductVariantId"
            INNER JOIN "MainProducts" p ON p."Id" = pv."ProductId"
            WHERE NOT si."IsDeleted"
              AND s."CreatedAt" >= @p0::timestamptz
              AND s."CreatedAt" < (@p1::date + 1)::timestamptz
            GROUP BY p."Title", pv."Barcode", pv."Id"
            ORDER BY quantity_sold DESC
            LIMIT 10
        )
        SELECT json_build_object(
            'summary', (SELECT row_to_json(summary) FROM summary),
            'daily', (SELECT COALESCE(json_agg(row_to_json(daily)), '[]') FROM daily),
            'topProducts', (SELECT COALESCE(json_agg(row_to_json(top_products)), '[]') FROM top_products)
        )::text AS "Value"
        """;

    public async Task<SalesReportDto> GetSalesReportAsync(SalesReportFilterDto filter)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var startDate = filter.StartDate.ToString("yyyy-MM-dd");
        var endDate = filter.EndDate.ToString("yyyy-MM-dd");

        var json = await dbContext.Database
            .SqlQueryRaw<string>(SalesReportQuery, startDate, endDate)
            .FirstOrDefaultAsync();

        if (string.IsNullOrEmpty(json))
            return new SalesReportDto(new SalesReportSummaryDto(0, 0, 0, 0), [], []);

        return ParseSalesReport(json);
    }

    private static SalesReportDto ParseSalesReport(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var s = root.GetProperty("summary");
        var totalSales = s.GetProperty("total_sales").GetInt32();
        var totalRevenue = s.GetProperty("total_revenue").GetDecimal();
        var totalItems = s.GetProperty("total_items").GetInt32();
        var totalTax = s.TryGetProperty("total_tax", out var taxProp) ? taxProp.GetDecimal() : 0m;
        var avg = totalSales > 0 ? totalRevenue / totalSales : 0;
        var summary = new SalesReportSummaryDto(totalSales, totalRevenue, avg, totalItems, totalTax);

        var daily = new List<DailySalesReportDto>();
        foreach (var item in root.GetProperty("daily").EnumerateArray())
        {
            daily.Add(new DailySalesReportDto(
                DateOnly.Parse(item.GetProperty("sale_date").GetString()!),
                item.GetProperty("sale_count").GetInt32(),
                item.GetProperty("revenue").GetDecimal()));
        }

        var topProducts = new List<TopSellingProductDto>();
        foreach (var item in root.GetProperty("topProducts").EnumerateArray())
        {
            topProducts.Add(new TopSellingProductDto(
                item.GetProperty("product_title").GetString() ?? "",
                item.GetProperty("variant_title").GetString() ?? "",
                item.GetProperty("quantity_sold").GetInt32(),
                item.GetProperty("revenue").GetDecimal()));
        }

        return new SalesReportDto(summary, daily, topProducts);
    }

    #endregion

    #region Inventory Report

    public async Task<InventoryReportDto> GetInventoryReportAsync(InventoryReportFilterDto filter)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var query = from bos in dbContext.BranchOfficeStocks
                    join pv in dbContext.ProductVariants on bos.ProductVariantId equals pv.Id
                    join p in dbContext.MainProducts on pv.ProductId equals p.Id
                    join bo in dbContext.BranchOffices on bos.BranchOfficeId equals bo.Id
                    where !pv.IsDeleted && !p.IsDeleted
                    select new { bos, pv, p, bo };

        if (filter.BranchOfficeId.HasValue)
            query = query.Where(x => x.bos.BranchOfficeId == filter.BranchOfficeId.Value);

        var data = await query.Select(x => new StockItemDto(
            x.pv.Id,
            x.p.Title ?? "",
            x.pv.Barcode ?? x.pv.Id.ToString(),
            x.pv.Barcode,
            x.bos.CurrentStock,
            x.bos.SoldQuantity,
            x.bo.Name ?? ""
        )).ToListAsync();

        var filtered = filter.StockFilter switch
        {
            StockFilter.LowStock => data.Where(x => x.CurrentStock > 0 && x.CurrentStock <= 5).ToList(),
            StockFilter.OutOfStock => data.Where(x => x.CurrentStock <= 0).ToList(),
            _ => data
        };

        var summary = new InventoryReportSummaryDto(
            TotalProducts: data.Count,
            TotalStock: data.Sum(x => x.CurrentStock),
            LowStockCount: data.Count(x => x.CurrentStock > 0 && x.CurrentStock <= 5),
            OutOfStockCount: data.Count(x => x.CurrentStock <= 0));

        return new InventoryReportDto(summary, filtered);
    }

    #endregion

    #region Marketplace Report

    public async Task<MarketplaceReportDto> GetMarketplaceReportAsync()
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var allItems = await dbContext.ProductMarketplaces
            .Include(pm => pm.Product)
            .Where(pm => !pm.IsDeleted && !pm.Product.IsDeleted)
            .ToListAsync();

        var summary = new MarketplaceReportSummaryDto(
            TotalProducts: allItems.Count,
            PublishedCount: allItems.Count(x => x.Status == Entity.Products.MarketplaceProductStatus.Published),
            PendingCount: allItems.Count(x => x.Status == Entity.Products.MarketplaceProductStatus.Pending),
            FailedCount: allItems.Count(x => x.Status == Entity.Products.MarketplaceProductStatus.Failed),
            RejectedCount: allItems.Count(x => x.Status == Entity.Products.MarketplaceProductStatus.Rejected),
            ApprovedCount: allItems.Count(x => x.IsApproved == true),
            ArchivedCount: allItems.Count(x => x.IsArchived == true));

        var failedProducts = allItems
            .Where(x => x.Status is Entity.Products.MarketplaceProductStatus.Failed
                                  or Entity.Products.MarketplaceProductStatus.Rejected)
            .OrderByDescending(x => x.UpdatedAt)
            .Take(50)
            .Select(x => new MarketplaceProductStatusDto(
                x.ProductId,
                x.Product.Title ?? "",
                x.Status.ToString(),
                x.StatusMessage,
                x.LastSyncedAt))
            .ToList();

        return new MarketplaceReportDto(summary, failedProducts);
    }

    #endregion
}

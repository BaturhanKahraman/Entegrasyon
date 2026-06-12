using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Extensions;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Reports;
using Entegrasyon.Entity.Invoicing;
using Entegrasyon.Entity.Sales;
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

        var raw = await query.Select(x => new
        {
            VariantId = x.pv.Id,
            ProductTitle = x.p.Title ?? "",
            x.pv.Name,
            Barcode = x.pv.Barcode,
            x.bos.BranchOfficeId,
            x.bos.CurrentStock,
            CumulativeSold = x.bos.SoldQuantity,
            // Stok değeri kaynağı: ProductVariant.CostPrice (birim maliyet, money kolonu) × adet.
            CostPrice = x.pv.CostPrice,
            BranchName = x.bo.Name ?? "",
            RawAttrs = x.pv.ProductVariantAttributes.Select(a => new { a.CategoryAttributeValue, a.CustomValue, a.IsVarianter, a.IsSlicer }).ToList()
        }).ToListAsync();

        // LastStockEntryDate — varyant+şube başına son stok GİRİŞİ (pozitif hareket: initial/return/inbound transfer).
        // Tek grouped aggregate (N+1 yok). Kapsayıcı index (BranchOfficeId, ProductVariantId, CreatedAt DESC)
        // WHERE NOT IsDeleted → index-tabanlı grouped MAX. StockAlert raporundaki ispatlı desenle aynı.
        var lastEntryQuery = dbContext.Set<Entity.Products.StockMovement>()
            .Where(m => !m.IsDeleted && m.Quantity > 0);
        if (filter.BranchOfficeId.HasValue)
            lastEntryQuery = lastEntryQuery.Where(m => m.BranchOfficeId == filter.BranchOfficeId.Value);

        var lastEntries = (await lastEntryQuery
                .GroupBy(m => new { m.BranchOfficeId, m.ProductVariantId })
                .Select(g => new { g.Key.BranchOfficeId, g.Key.ProductVariantId, LastAt = g.Max(m => m.CreatedAt) })
                .ToListAsync())
            .ToDictionary(x => (x.BranchOfficeId, x.ProductVariantId), x => DateOnly.FromDateTime(x.LastAt.UtcDateTime));

        // Dönem-bazlı SoldQuantity: dönem verildiyse satış hareketlerinden (Sale/MarketplaceSale) topla;
        // verilmediyse BranchOfficeStock.SoldQuantity kümülatif sayacı kullanılır.
        var hasPeriod = filter.StartDate.HasValue && filter.EndDate.HasValue;
        Dictionary<(int, Guid), int> periodSold = new();
        if (hasPeriod)
        {
            var startUtc = filter.StartDate!.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var endExclusiveUtc = filter.EndDate!.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

            var soldQuery = dbContext.Set<Entity.Products.StockMovement>()
                .Where(m => !m.IsDeleted
                            && (m.Type == Entity.Products.StockMovementType.Sale
                                || m.Type == Entity.Products.StockMovementType.MarketplaceSale)
                            && m.CreatedAt >= startUtc
                            && m.CreatedAt < endExclusiveUtc);
            if (filter.BranchOfficeId.HasValue)
                soldQuery = soldQuery.Where(m => m.BranchOfficeId == filter.BranchOfficeId.Value);

            periodSold = (await soldQuery
                    .GroupBy(m => new { m.BranchOfficeId, m.ProductVariantId })
                    // Satış hareketi Quantity negatif (azalış) → satılan adet = |Σ Quantity|.
                    .Select(g => new { g.Key.BranchOfficeId, g.Key.ProductVariantId, Sold = g.Sum(m => m.Quantity) })
                    .ToListAsync())
                .ToDictionary(x => (x.BranchOfficeId, x.ProductVariantId), x => Math.Abs(x.Sold));
        }

        var data = raw.Select(r =>
        {
            var key = (r.BranchOfficeId, r.VariantId);
            var sold = hasPeriod
                ? (periodSold.TryGetValue(key, out var ps) ? ps : 0)
                : r.CumulativeSold;
            var lastEntry = lastEntries.TryGetValue(key, out var le) ? (DateOnly?)le : null;

            return new StockItemDto(
                r.VariantId,
                r.ProductTitle,
                VariantNameExtensions.ResolveDisplayName(
                    r.Name,
                    r.RawAttrs.Select((a, i) => new VariantAttributeLite(a.CategoryAttributeValue, a.CustomValue, a.IsVarianter, a.IsSlicer, i)),
                    r.ProductTitle),
                r.Barcode,
                r.CurrentStock,
                sold,
                r.BranchName,
                StockValue: r.CostPrice * r.CurrentStock,
                LastStockEntryDate: lastEntry);
        }).ToList();

        var filtered = filter.StockFilter switch
        {
            StockFilter.LowStock => data.Where(x => x.CurrentStock > 0 && x.CurrentStock <= 5).ToList(),
            StockFilter.OutOfStock => data.Where(x => x.CurrentStock <= 0).ToList(),
            _ => data
        };

        // Satılmayan stok tespiti: dönemde (veya kümülatif) SoldQuantity == 0 olan satırlar.
        if (filter.UnsoldOnly)
            filtered = filtered.Where(x => x.SoldQuantity == 0).ToList();

        var summary = new InventoryReportSummaryDto(
            TotalProducts: data.Count,
            TotalStock: data.Sum(x => x.CurrentStock),
            LowStockCount: data.Count(x => x.CurrentStock > 0 && x.CurrentStock <= 5),
            OutOfStockCount: data.Count(x => x.CurrentStock <= 0),
            StockValue: data.Sum(x => x.StockValue));

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

    #region Profit/Loss Report

    private const string ProfitLossQuery = """
        WITH order_data AS (
            SELECT
                o."MarketPlaceId",
                mp."Name" AS marketplace_name,
                COALESCE(SUM(oi."UnitPrice" * oi."Quantity"), 0) AS revenue,
                COALESCE(SUM(oi."Quantity"), 0)::int AS total_items
            FROM "Orders" o
            INNER JOIN "OrderItems" oi ON oi."OrderId" = o."Id" AND NOT oi."IsDeleted"
            LEFT JOIN "MarketPlaces" mp ON mp."Id" = o."MarketPlaceId"
            WHERE NOT o."IsDeleted"
              AND o."CreatedAt" >= @p0::timestamptz
              AND o."CreatedAt" < (@p1::date + 1)::timestamptz
              AND (@p2::int IS NULL OR o."MarketPlaceId" = @p2::int)
            GROUP BY o."MarketPlaceId", mp."Name"
        ),
        commission_data AS (
            SELECT
                od.marketplace_name,
                od."MarketPlaceId",
                od.revenue,
                COALESCE(
                    od.revenue * mcr."CommissionPercent" / 100.0,
                    od.revenue * 0.12
                ) AS commission
            FROM order_data od
            LEFT JOIN "MarketplaceCommissionRates" mcr
                ON mcr."MarketPlaceId" = od."MarketPlaceId" AND mcr."IsDefault" = true AND NOT mcr."IsDeleted"
        ),
        sale_tax AS (
            SELECT COALESCE(SUM(
                si."UnitPrice" * si."Quantity" * (1 - si."DiscountPercent" / 100.0)
                * si."TaxPercentage" / 100.0
            ), 0) AS total_tax
            FROM "SaleItems" si
            INNER JOIN "Sales" s ON s."Id" = si."SaleId" AND NOT s."IsDeleted"
            WHERE NOT si."IsDeleted"
              AND s."CreatedAt" >= @p0::timestamptz
              AND s."CreatedAt" < (@p1::date + 1)::timestamptz
        )
        SELECT json_build_object(
            'revenue', COALESCE((SELECT SUM(revenue) FROM commission_data), 0),
            'commission', COALESCE((SELECT SUM(commission) FROM commission_data), 0),
            'tax', (SELECT total_tax FROM sale_tax),
            'byMarketplace', COALESCE((
                SELECT json_agg(json_build_object(
                    'marketPlaceId', COALESCE("MarketPlaceId", 0),
                    'marketPlaceName', COALESCE(marketplace_name, 'Diger'),
                    'revenue', revenue,
                    'commission', commission,
                    'netProfit', revenue - commission
                ))
                FROM commission_data
            ), '[]')
        )::text AS "Value"
        """;

    public async Task<ProfitLossReportDto> GetProfitLossReportAsync(ProfitLossReportFilterDto filter)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var startDate = filter.StartDate.ToString("yyyy-MM-dd");
        var endDate = filter.EndDate.ToString("yyyy-MM-dd");
        object? mpId = filter.MarketPlaceId.HasValue ? filter.MarketPlaceId.Value : null;

        var json = await dbContext.Database
            .SqlQueryRaw<string>(ProfitLossQuery, startDate, endDate, mpId!)
            .FirstOrDefaultAsync();

        if (string.IsNullOrEmpty(json))
            return new ProfitLossReportDto(0, 0, 0, 0, 0, 0, 0, []);

        return ParseProfitLossReport(json);
    }

    private static ProfitLossReportDto ParseProfitLossReport(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var revenue = root.GetProperty("revenue").GetDecimal();
        var commission = root.GetProperty("commission").GetDecimal();
        var tax = root.GetProperty("tax").GetDecimal();
        var netProfit = revenue - commission - tax;
        var profitMargin = revenue > 0 ? Math.Round(netProfit / revenue * 100, 2) : 0;

        var byMarketplace = new List<ProfitLossByMarketplaceDto>();
        foreach (var item in root.GetProperty("byMarketplace").EnumerateArray())
        {
            byMarketplace.Add(new ProfitLossByMarketplaceDto(
                item.GetProperty("marketPlaceId").GetInt32(),
                item.GetProperty("marketPlaceName").GetString() ?? "",
                item.GetProperty("revenue").GetDecimal(),
                item.GetProperty("commission").GetDecimal(),
                item.GetProperty("netProfit").GetDecimal()));
        }

        return new ProfitLossReportDto(
            Revenue: revenue,
            CostOfGoods: 0, // Maliyet bilgisi entity'de mevcut degil, ileride eklenecek
            MarketplaceCommission: commission,
            CargoExpense: 0, // Kargo gideri ileride eklenecek
            TaxAmount: tax,
            NetProfit: netProfit,
            ProfitMargin: profitMargin,
            ByMarketplace: byMarketplace);
    }

    #endregion

    #region Product Performance

    private const string ProductPerformanceQuery = """
        SELECT
            p."Id" AS product_id,
            p."Title" AS product_name,
            COALESCE(SUM(si."Quantity"), 0)::int AS total_sold,
            COALESCE(SUM(si."UnitPrice" * si."Quantity"), 0) AS total_revenue,
            0::decimal AS return_rate,
            0::decimal AS average_rating,
            CASE WHEN COALESCE(SUM(bos."CurrentStock"), 0) > 0
                THEN ROUND(COALESCE(SUM(si."Quantity"), 0)::decimal / GREATEST(SUM(bos."CurrentStock"), 1), 2)
                ELSE 0
            END AS stock_turnover_rate
        FROM "MainProducts" p
        LEFT JOIN "ProductVariants" pv ON pv."ProductId" = p."Id" AND NOT pv."IsDeleted"
        LEFT JOIN "SaleItems" si ON si."ProductVariantId" = pv."Id" AND NOT si."IsDeleted"
            AND EXISTS (
                SELECT 1 FROM "Sales" s
                WHERE s."Id" = si."SaleId" AND NOT s."IsDeleted"
                  AND s."CreatedAt" >= @p0::timestamptz
                  AND s."CreatedAt" < (@p1::date + 1)::timestamptz
            )
        LEFT JOIN "BranchOfficeStocks" bos ON bos."ProductVariantId" = pv."Id"
        WHERE NOT p."IsDeleted"
        GROUP BY p."Id", p."Title"
        ORDER BY total_sold DESC
        LIMIT @p2
        """;

    public async Task<List<ProductPerformanceDto>> GetProductPerformanceAsync(ProductPerformanceFilterDto filter)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var startDate = filter.StartDate.ToString("yyyy-MM-dd");
        var endDate = filter.EndDate.ToString("yyyy-MM-dd");
        var topN = filter.TopN ?? 50;

        var query = $"""
            SELECT json_agg(row_to_json(t))::text AS "Value"
            FROM ({ProductPerformanceQuery}) t
            """;

        var json = await dbContext.Database
            .SqlQueryRaw<string>(query, startDate, endDate, topN)
            .FirstOrDefaultAsync();

        if (string.IsNullOrEmpty(json) || json == "null")
            return [];

        return ParseProductPerformance(json);
    }

    private static List<ProductPerformanceDto> ParseProductPerformance(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var result = new List<ProductPerformanceDto>();

        foreach (var item in doc.RootElement.EnumerateArray())
        {
            result.Add(new ProductPerformanceDto(
                Guid.Parse(item.GetProperty("product_id").GetString()!),
                item.GetProperty("product_name").GetString() ?? "",
                item.GetProperty("total_sold").GetInt32(),
                item.GetProperty("total_revenue").GetDecimal(),
                item.GetProperty("return_rate").GetDecimal(),
                item.GetProperty("average_rating").GetDecimal(),
                item.GetProperty("stock_turnover_rate").GetDecimal()));
        }

        return result;
    }

    #endregion

    #region Stock Alerts

    // Seviye türetimi (DB-translatable arithmetic ile aynı mantık):
    //   CurrentStock <= 0                     → Critical (Tükendi de bu sınıfa girer)
    //   DaysUntilStockout <= 3                → Critical
    //   CurrentStock <= MinimumStockThreshold → Low
    // DaysUntilStockout = CurrentStock / max(1, SoldQuantity/30).
    // Sorgu zaten CurrentStock <= threshold filtrelediği için dönen tüm satırlar Low/Critical olur.

    public async Task<StockAlertReportDto> GetStockAlertReportAsync(StockAlertPaginatedRequest request)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var threshold = request.MinimumStockThreshold;

        // Eşik altı + branch filtresi. Branch join (BranchOffice) şube adı + filtre için.
        var baseQuery = from bos in dbContext.BranchOfficeStocks
                        join pv in dbContext.ProductVariants on bos.ProductVariantId equals pv.Id
                        join p in dbContext.MainProducts on pv.ProductId equals p.Id
                        join bo in dbContext.BranchOffices on bos.BranchOfficeId equals bo.Id
                        where !pv.IsDeleted && !p.IsDeleted
                              && bos.CurrentStock <= threshold
                        select new { bos, pv, p, bo };

        if (request.BranchOfficeId.HasValue)
            baseQuery = baseQuery.Where(x => x.bos.BranchOfficeId == request.BranchOfficeId.Value);

        // DB-side seviye ordinali (0=Sufficient,1=Low,2=Critical) — filtre + KPI özet için.
        // daysUntilStockout = CurrentStock / max(1, SoldQuantity/30) (integer aritmetiği).
        var leveled = baseQuery.Select(x => new
        {
            x.bos,
            x.pv,
            x.bo,
            ProductTitle = x.p.Title ?? "",
            RawAttrs = x.pv.ProductVariantAttributes
                .Select(a => new { a.CategoryAttributeValue, a.CustomValue, a.IsVarianter, a.IsSlicer }).ToList(),
            DaysUntilStockout = x.bos.CurrentStock > 0
                ? x.bos.CurrentStock / (x.bos.SoldQuantity / 30 > 1 ? x.bos.SoldQuantity / 30 : 1)
                : 0
        });

        // LevelOrdinal: 2=Critical, 1=Low. (Filtre öncesi CurrentStock<=threshold garanti edildiği için Sufficient gelmez.)
        var withLevel = leveled.Select(x => new
        {
            x.bos,
            x.pv,
            x.bo,
            x.ProductTitle,
            x.RawAttrs,
            x.DaysUntilStockout,
            LevelOrdinal = (x.bos.CurrentStock <= 0 || x.DaysUntilStockout <= 3) ? 2 : 1
        });

        if (request.AlertLevel.HasValue)
        {
            var wanted = (int)request.AlertLevel.Value;
            withLevel = withLevel.Where(x => x.LevelOrdinal == wanted);
        }

        var totalCount = await withLevel.CountAsync();

        // KPI özet — TÜM filtrelenmiş küme üzerinden (sayfa değil), tek round-trip aggregate.
        var summaryRaw = await withLevel
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Critical = g.Count(x => x.LevelOrdinal == 2),
                Low = g.Count(x => x.LevelOrdinal == 1),
                OutOfStock = g.Count(x => x.bos.CurrentStock <= 0),
                Total = g.Count()
            })
            .FirstOrDefaultAsync();

        var summary = summaryRaw is null
            ? new StockAlertSummaryDto(0, 0, 0, 0)
            : new StockAlertSummaryDto(summaryRaw.Critical, summaryRaw.Low, summaryRaw.OutOfStock, summaryRaw.Total);

        // Sayfa: indexli OrderBy (CurrentStock) — en kritik önce.
        var page = await withLevel
            .OrderBy(x => x.bos.CurrentStock)
            .ThenBy(x => x.pv.Id)
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        // LastStockEntryDate — sadece sayfadaki varyantlar için batched (pozitif/initial hareket max CreatedAt).
        var pageVariantIds = page.Where(x => x.bos.ProductVariantId.HasValue)
            .Select(x => x.bos.ProductVariantId!.Value).Distinct().ToList();

        var lastEntries = pageVariantIds.Count == 0
            ? new Dictionary<(int, Guid), DateTime>()
            : (await dbContext.Set<Entity.Products.StockMovement>()
                .Where(m => !m.IsDeleted
                            && m.Quantity > 0
                            && pageVariantIds.Contains(m.ProductVariantId))
                .GroupBy(m => new { m.BranchOfficeId, m.ProductVariantId })
                .Select(g => new { g.Key.BranchOfficeId, g.Key.ProductVariantId, LastAt = g.Max(m => m.CreatedAt) })
                .ToListAsync())
              .ToDictionary(x => (x.BranchOfficeId, x.ProductVariantId), x => x.LastAt.UtcDateTime);

        var items = page.Select(x =>
        {
            var suggestedOrder = Math.Max(threshold * 3 - x.bos.CurrentStock, 0);
            var level = x.LevelOrdinal == 2 ? StockAlertLevel.Critical : StockAlertLevel.Low;

            var displayName = VariantNameExtensions.ResolveDisplayName(
                x.pv.Name,
                x.RawAttrs.Select((a, i) => new VariantAttributeLite(a.CategoryAttributeValue, a.CustomValue, a.IsVarianter, a.IsSlicer, i)),
                x.ProductTitle);

            DateTime? lastEntry = x.bos.ProductVariantId.HasValue
                && lastEntries.TryGetValue((x.bos.BranchOfficeId, x.bos.ProductVariantId.Value), out var d)
                ? d : null;

            return new StockAlertDto(
                x.pv.Id,
                x.pv.Barcode,
                x.ProductTitle,
                displayName,
                x.bos.CurrentStock,
                threshold,
                x.DaysUntilStockout,
                suggestedOrder,
                level,
                x.bo.Id,
                x.bo.Name ?? "",
                lastEntry);
        }).ToList();

        return new StockAlertReportDto(
            summary,
            new Pageable<StockAlertDto>(items, request.PageIndex, request.PageSize, totalCount));
    }

    #endregion

    #region Marketplace Summary

    // En çok satan ürünler (pazaryeri bazında): OrderItems → ProductVariant → MainProducts.
    // Etiket: ürün başlığı (eşleşen yerel ürün) ya da MerchantSku/Barcode fallback (eşleşmemiş
    // pazaryeri satırı kaybolmasın). Adede göre azalan, en fazla 3. Pazaryeri başına izole
    // (LATERAL alt-sorgu o."MarketPlaceId"e bağlı). Büyük tablo (Orders/OrderItems) — index'li
    // join (OrderItems.OrderId FK, Orders.MarketPlaceId+CreatedAt) DB Master review tetikleyici.
    private const string MarketplaceSummaryQuery = """
        SELECT json_agg(row_to_json(t))::text AS "Value"
        FROM (
            SELECT
                COALESCE(o."MarketPlaceId", 0) AS marketplace_id,
                COALESCE(mp."Name", 'Diger') AS marketplace_name,
                COUNT(DISTINCT o."Id")::int AS total_orders,
                COALESCE(SUM(oi."UnitPrice" * oi."Quantity"), 0) AS total_revenue,
                COALESCE(
                    SUM(oi."UnitPrice" * oi."Quantity") * COALESCE(mcr."CommissionPercent", 12) / 100.0,
                    0
                ) AS commission_paid,
                CASE WHEN COUNT(DISTINCT o."Id") > 0
                    THEN ROUND(COALESCE(SUM(oi."UnitPrice" * oi."Quantity"), 0) / COUNT(DISTINCT o."Id"), 2)
                    ELSE 0
                END AS average_order_value,
                COALESCE((
                    SELECT json_agg(tp.product_label ORDER BY tp.qty DESC)
                    FROM (
                        SELECT
                            COALESCE(p2."Title", oi2."MerchantSku", oi2."Barcode", 'Bilinmeyen Ürün') AS product_label,
                            SUM(oi2."Quantity")::int AS qty
                        FROM "Orders" o2
                        INNER JOIN "OrderItems" oi2 ON oi2."OrderId" = o2."Id" AND NOT oi2."IsDeleted"
                        LEFT JOIN "ProductVariants" pv2 ON pv2."Id" = oi2."ProductId"
                        LEFT JOIN "MainProducts" p2 ON p2."Id" = pv2."ProductId"
                        WHERE NOT o2."IsDeleted"
                          AND o2."MarketPlaceId" IS NOT DISTINCT FROM o."MarketPlaceId"
                          AND o2."CreatedAt" >= @p0::timestamptz
                          AND o2."CreatedAt" < (@p1::date + 1)::timestamptz
                        GROUP BY 1
                        ORDER BY qty DESC
                        LIMIT 3
                    ) tp
                ), '[]') AS top_selling_products
            FROM "Orders" o
            INNER JOIN "OrderItems" oi ON oi."OrderId" = o."Id" AND NOT oi."IsDeleted"
            LEFT JOIN "MarketPlaces" mp ON mp."Id" = o."MarketPlaceId"
            LEFT JOIN "MarketplaceCommissionRates" mcr
                ON mcr."MarketPlaceId" = o."MarketPlaceId" AND mcr."IsDefault" = true AND NOT mcr."IsDeleted"
            WHERE NOT o."IsDeleted"
              AND o."CreatedAt" >= @p0::timestamptz
              AND o."CreatedAt" < (@p1::date + 1)::timestamptz
            GROUP BY o."MarketPlaceId", mp."Name", mcr."CommissionPercent"
            ORDER BY total_revenue DESC
        ) t
        """;

    public async Task<List<MarketplaceSummaryDto>> GetMarketplaceSummaryAsync(MarketplaceSummaryFilterDto filter)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var startDate = filter.StartDate.ToString("yyyy-MM-dd");
        var endDate = filter.EndDate.ToString("yyyy-MM-dd");

        var json = await dbContext.Database
            .SqlQueryRaw<string>(MarketplaceSummaryQuery, startDate, endDate)
            .FirstOrDefaultAsync();

        if (string.IsNullOrEmpty(json) || json == "null")
            return [];

        return ParseMarketplaceSummary(json);
    }

    private static List<MarketplaceSummaryDto> ParseMarketplaceSummary(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var result = new List<MarketplaceSummaryDto>();

        foreach (var item in doc.RootElement.EnumerateArray())
        {
            var topSelling = new List<string>();
            if (item.TryGetProperty("top_selling_products", out var tsp)
                && tsp.ValueKind == JsonValueKind.Array)
            {
                foreach (var p in tsp.EnumerateArray())
                {
                    var label = p.GetString();
                    if (!string.IsNullOrEmpty(label))
                        topSelling.Add(label);
                }
            }

            result.Add(new MarketplaceSummaryDto(
                item.GetProperty("marketplace_id").GetInt32(),
                item.GetProperty("marketplace_name").GetString() ?? "",
                item.GetProperty("total_orders").GetInt32(),
                item.GetProperty("total_revenue").GetDecimal(),
                item.GetProperty("commission_paid").GetDecimal(),
                item.GetProperty("average_order_value").GetDecimal(),
                topSelling));
        }

        return result;
    }

    #endregion

    #region Top Selling / Slow Moving Products

    public async Task<List<TopSellingProductDto>> GetTopSellingProductsAsync(
        DateOnly startDate, DateOnly endDate, int top = 10)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var startDateTime = startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endDateTime = endDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(1);

        var result = await (
            from s in dbContext.Sales
            where !s.IsDeleted
                  && s.CreatedAt >= startDateTime
                  && s.CreatedAt < endDateTime
            from si in s.SaleItems
            where !si.IsDeleted
            join pv in dbContext.ProductVariants on si.ProductVariantId equals pv.Id
            join p in dbContext.MainProducts on pv.ProductId equals p.Id
            group new { si, pv, p } by new { p.Title, Barcode = pv.Barcode ?? pv.Id.ToString() }
            into g
            orderby g.Sum(x => x.si.Quantity) descending
            select new TopSellingProductDto(
                g.Key.Title ?? "",
                g.Key.Barcode,
                g.Sum(x => x.si.Quantity),
                g.Sum(x => x.si.UnitPrice * x.si.Quantity))
        ).Take(top).ToListAsync();

        return result;
    }

    public async Task<List<ProductPerformanceDto>> GetSlowMovingProductsAsync(
        DateOnly startDate, DateOnly endDate, int top = 10)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var startDateTime = startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endDateTime = endDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(1);

        // Ilk once satis adetlerini hesapla
        var salesByVariant = await (
            from s in dbContext.Sales
            where !s.IsDeleted && s.CreatedAt >= startDateTime && s.CreatedAt < endDateTime
            from si in s.SaleItems
            where !si.IsDeleted
            group si by si.ProductVariantId into g
            select new { ProductVariantId = g.Key, TotalSold = g.Sum(x => x.Quantity) }
        ).ToListAsync();

        var salesDict = salesByVariant.ToDictionary(x => x.ProductVariantId, x => x.TotalSold);

        var products = await (
            from p in dbContext.MainProducts
            join pv in dbContext.ProductVariants on p.Id equals pv.ProductId
            join bos in dbContext.BranchOfficeStocks on pv.Id equals bos.ProductVariantId
            where !p.IsDeleted && !pv.IsDeleted && bos.CurrentStock > 0
            select new { ProductId = p.Id, p.Title, bos.CurrentStock, VariantId = pv.Id }
        ).ToListAsync();

        var result = products
            .GroupBy(x => new { x.ProductId, x.Title })
            .Select(g =>
            {
                var totalSold = g.Sum(x => salesDict.GetValueOrDefault(x.VariantId, 0));
                var totalStock = g.Sum(x => x.CurrentStock);
                var turnover = totalStock > 0 && totalSold > 0
                    ? Math.Round((decimal)totalSold / totalStock, 2)
                    : 0m;

                return new ProductPerformanceDto(
                    g.Key.ProductId,
                    g.Key.Title ?? "",
                    totalSold,
                    0m, 0m, 0m,
                    turnover);
            })
            .OrderBy(x => x.TotalSold)
            .Take(top)
            .ToList();

        return result;
    }

    #endregion

    #region Tax Report

    public async Task<VatDeclarationDto> GetVatDeclarationAsync(DateOnly startDate, DateOnly endDate)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        var startTs = new DateTimeOffset(startDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var endTs = new DateTimeOffset(endDate.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);

        // Sadece KESİLEN faturalar (Sent/Accepted), dönem içi; satırları KDV oranına göre grupla.
        // Matrah = LineTotal - TaxAmount (LineTotal KDV dahil).
        var lines = await db.Set<EInvoice>()
            .AsNoTracking()
            .Where(i => !i.IsDeleted
                        && (i.Status == EInvoiceStatus.Sent || i.Status == EInvoiceStatus.Accepted)
                        && i.IssueDate >= startTs && i.IssueDate <= endTs)
            .SelectMany(i => i.Lines)
            .Where(l => !l.IsDeleted)
            .GroupBy(l => l.TaxRate)
            .OrderBy(g => g.Key)
            .Select(g => new VatDeclarationLineDto(
                g.Key,
                g.Sum(l => l.LineTotal - l.TaxAmount),
                g.Sum(l => l.TaxAmount),
                g.Count()))
            .ToListAsync();

        return new VatDeclarationDto(lines, lines.Sum(l => l.TaxBase), lines.Sum(l => l.VatAmount));
    }

    public async Task<InvoiceTypeBreakdownDto> GetInvoiceTypeBreakdownAsync(DateOnly startDate, DateOnly endDate)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        var startTs = new DateTimeOffset(startDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var endTs = new DateTimeOffset(endDate.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);

        // Kesilen faturalar (Sent/Accepted), dönem içi — türe göre adet + GrandTotal.
        var invoiceGroups = await db.Set<EInvoice>()
            .AsNoTracking()
            .Where(i => !i.IsDeleted
                        && (i.Status == EInvoiceStatus.Sent || i.Status == EInvoiceStatus.Accepted)
                        && i.IssueDate >= startTs && i.IssueDate <= endTs)
            .GroupBy(i => i.InvoiceType)
            .Select(g => new { Type = g.Key, Count = g.Count(), Total = g.Sum(i => i.GrandTotal) })
            .ToListAsync();

        var eFatura = invoiceGroups.FirstOrDefault(g => g.Type == EInvoiceType.EFatura);
        var eArsiv = invoiceGroups.FirstOrDefault(g => g.Type == EInvoiceType.EArsiv);

        // Faturası kesilmiş (Sent/Accepted) satışların Id'leri.
        var invoicedSaleIds = db.Set<EInvoice>()
            .Where(i => !i.IsDeleted
                        && (i.Status == EInvoiceStatus.Sent || i.Status == EInvoiceStatus.Accepted)
                        && i.SaleId != null)
            .Select(i => i.SaleId!.Value);

        // Faturasız: dönem içi iptal-olmayan satışlardan faturası bulunmayanlar.
        // Tutar = SUM(UnitPrice * Quantity * (1 - DiscountPercent/100)) — satış raporu geliriyle aynı formül.
        var uninvoiced = await db.Set<Sale>()
            .AsNoTracking()
            .Where(s => !s.IsDeleted
                        && s.SaleStatus != SaleStatus.Cancelled
                        && s.SaleDate >= startTs && s.SaleDate <= endTs
                        && !invoicedSaleIds.Contains(s.Id))
            .Select(s => s.SaleItems
                .Where(si => !si.IsDeleted)
                .Sum(si => (double)si.UnitPrice * si.Quantity * (1 - si.DiscountPercent / 100.0)))
            .ToListAsync();

        var uninvoicedSegment = new InvoiceTypeSegmentDto(
            uninvoiced.Count,
            Math.Round((decimal)uninvoiced.Sum(), 2));

        return new InvoiceTypeBreakdownDto(
            new InvoiceTypeSegmentDto(eFatura?.Count ?? 0, eFatura?.Total ?? 0m),
            new InvoiceTypeSegmentDto(eArsiv?.Count ?? 0, eArsiv?.Total ?? 0m),
            uninvoicedSegment);
    }

    #endregion
}

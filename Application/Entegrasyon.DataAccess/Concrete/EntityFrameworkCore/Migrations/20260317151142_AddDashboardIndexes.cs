using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <summary>
    /// Dashboard performans optimizasyonları:
    /// 1. Partial indexes (Sales, SaleItems, Orders)
    /// 2. B-tree index (BranchOfficeStocks, Logs)
    /// 3. Materialized View (mv_product_stock_summary — ürün stok agregasyonu)
    /// </summary>
    public partial class AddDashboardIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ═══════════════════════════════════════════════════════════
            // 1. PARTIAL INDEXES — Dashboard Sorguları
            // ═══════════════════════════════════════════════════════════

            // Sales: bugünkü satışlar (CreatedAt >= CURRENT_DATE filtresi)
            migrationBuilder.Sql(
                @"CREATE INDEX ""IX_Sales_CreatedAt_Active""
                  ON ""Sales"" (""CreatedAt"" DESC)
                  WHERE NOT ""IsDeleted"";");

            // SaleItems: satış geliri hesabı (SaleId join)
            migrationBuilder.Sql(
                @"CREATE INDEX ""IX_SaleItems_SaleId_Active""
                  ON ""SaleItems"" (""SaleId"")
                  WHERE NOT ""IsDeleted"";");

            // Orders: bekleyen siparişler (MarketplaceOrderStatus IN filtresi)
            migrationBuilder.Sql(
                @"CREATE INDEX ""IX_Orders_MarketplaceOrderStatus_Active""
                  ON ""Orders"" (""MarketplaceOrderStatus"")
                  WHERE NOT ""IsDeleted"";");

            // BranchOfficeStocks: düşük stok hesabı — composite PK tek başına ProductVariantId aramasını kapsamaz
            migrationBuilder.Sql(
                @"CREATE INDEX IF NOT EXISTS ""IX_BranchOfficeStocks_ProductVariantId""
                  ON ""BranchOfficeStocks"" (""ProductVariantId"");");

            // Logs: ORDER BY CreatedAt DESC LIMIT 10 — mevcut BRIN sort+limit için yetersiz
            migrationBuilder.Sql(
                @"CREATE INDEX ""IX_Logs_CreatedAt_Desc""
                  ON ""Logs"" (""CreatedAt"" DESC);");

            // ═══════════════════════════════════════════════════════════
            // 2. MATERIALIZED VIEW — Ürün Stok Özeti
            // ═══════════════════════════════════════════════════════════

            migrationBuilder.Sql(@"
                CREATE MATERIALIZED VIEW mv_product_stock_summary AS
                SELECT
                    pv.""ProductId"",
                    COALESCE(SUM(bos.""CurrentStock""), 0)::int AS ""TotalStock""
                FROM ""ProductVariants"" pv
                LEFT JOIN ""BranchOfficeStocks"" bos ON bos.""ProductVariantId"" = pv.""Id""
                WHERE NOT pv.""IsDeleted""
                GROUP BY pv.""ProductId""
                WITH DATA;");

            migrationBuilder.Sql(
                @"CREATE UNIQUE INDEX ON mv_product_stock_summary (""ProductId"");");

            migrationBuilder.Sql(
                @"CREATE INDEX ""IX_mv_product_stock_TotalStock""
                  ON mv_product_stock_summary (""TotalStock"");");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Materialized View
            migrationBuilder.Sql(@"DROP MATERIALIZED VIEW IF EXISTS mv_product_stock_summary;");

            // Indexes
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Logs_CreatedAt_Desc"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_BranchOfficeStocks_ProductVariantId"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Orders_MarketplaceOrderStatus_Active"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_SaleItems_SaleId_Active"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Sales_CreatedAt_Active"";");
        }
    }
}

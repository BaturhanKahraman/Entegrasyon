using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <summary>
    /// PostgreSQL-native optimizasyonlar:
    /// 1. Partial indexes (Orders, ProductVariants, CategoryAttributeCategories)
    /// 2. pg_trgm extension + trigram indexes (Categories, CategoryAttributes)
    /// 3. Recursive CTE fonksiyonu (kategori hiyerarşisi)
    /// 4. BRIN indexes (StockMovements, Logs, ProductActivityLogs)
    /// 5. Covering indexes (Categories, Products)
    /// 6. Materialized View (mv_category_summary — kategori stok agregasyonu)
    /// </summary>
    public partial class PostgresOptimizations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ═══════════════════════════════════════════════════════════
            // 1. PARTIAL INDEXES
            // ═══════════════════════════════════════════════════════════

            // Orders: Trendyol import deduplication — ShipmentPackageId ile AnyAsync kontrolü
            migrationBuilder.Sql(
                @"CREATE UNIQUE INDEX ""IX_Orders_ShipmentPackageId_Active""
                  ON ""Orders"" (""ShipmentPackageId"")
                  WHERE NOT ""IsDeleted"" AND ""ShipmentPackageId"" IS NOT NULL;");

            // Orders: MarketPlaceId + OrderDate filtreleme — GetOrdersAsync sorgusunda
            migrationBuilder.Sql(
                @"CREATE INDEX ""IX_Orders_MarketPlaceId_OrderDate_Active""
                  ON ""Orders"" (""MarketPlaceId"", ""OrderDate"" DESC)
                  WHERE NOT ""IsDeleted"";");

            // ProductVariants: Mevcut unique index silinmiş kayıtları da kapsıyor — partial ile değiştir
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_ProductVariants_Barcode"";");
            migrationBuilder.Sql(
                @"CREATE UNIQUE INDEX ""IX_ProductVariants_Barcode_Active""
                  ON ""ProductVariants"" (""Barcode"")
                  WHERE NOT ""IsDeleted"";");

            // CategoryAttributeCategories: Junction tablosu — CategoryId ile 6+ yerde filtreleniyor
            migrationBuilder.Sql(
                @"CREATE INDEX ""IX_CategoryAttributeCategories_CategoryId""
                  ON ""CategoryAttributeCategories"" (""CategoryId"");");

            // ═══════════════════════════════════════════════════════════
            // 2. pg_trgm EXTENSION + TRIGRAM INDEXES
            // ═══════════════════════════════════════════════════════════

            migrationBuilder.Sql(@"CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            // CategoryAttributes: ILIKE '%arama%' sorguları için trigram index
            migrationBuilder.Sql(
                @"CREATE INDEX ""IX_CategoryAttributes_Humanized_Trgm""
                  ON ""CategoryAttributes"" USING gin (""CategoryAttributeHumanized"" gin_trgm_ops)
                  WHERE NOT ""IsDeleted"";");

            // CategoryAttributes: Key alanı da aranıyor
            migrationBuilder.Sql(
                @"CREATE INDEX ""IX_CategoryAttributes_Key_Trgm""
                  ON ""CategoryAttributes"" USING gin (""CategoryAttributeKey"" gin_trgm_ops)
                  WHERE NOT ""IsDeleted"";");

            // Categories: Kategori ismi ile ILIKE araması
            migrationBuilder.Sql(
                @"CREATE INDEX ""IX_Categories_Name_Trgm""
                  ON ""Categories"" USING gin (""Name"" gin_trgm_ops)
                  WHERE NOT ""IsDeleted"";");

            // ═══════════════════════════════════════════════════════════
            // 3. RECURSIVE CTE — Kategori Hiyerarşi Fonksiyonu
            // ═══════════════════════════════════════════════════════════

            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION fn_get_descendant_ids(parent_id int)
                RETURNS SETOF int AS $$
                    WITH RECURSIVE descendants AS (
                        SELECT ""Id"" FROM ""Categories""
                        WHERE ""SuperCategoryId"" = parent_id AND NOT ""IsDeleted""
                        UNION ALL
                        SELECT c.""Id"" FROM ""Categories"" c
                        INNER JOIN descendants d ON c.""SuperCategoryId"" = d.""Id""
                        WHERE NOT c.""IsDeleted""
                    )
                    SELECT ""Id"" FROM descendants;
                $$ LANGUAGE SQL STABLE;");

            // ═══════════════════════════════════════════════════════════
            // 4. BRIN INDEXES — Zaman Serisi Tablolar
            // ═══════════════════════════════════════════════════════════

            // StockMovements: Mevcut B-tree → BRIN (append-only audit trail)
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_StockMovements_CreatedAt"";");
            migrationBuilder.Sql(
                @"CREATE INDEX ""IX_StockMovements_CreatedAt_BRIN""
                  ON ""StockMovements"" USING brin (""CreatedAt"");");

            // ApplicationLogs (Logs tablosu): Yüksek yazma hacmi, zaman bazlı sorgulama
            migrationBuilder.Sql(
                @"CREATE INDEX ""IX_Logs_CreatedAt_BRIN""
                  ON ""Logs"" USING brin (""CreatedAt"");");

            // ProductActivityLogs: Ürün timeline — append-only
            migrationBuilder.Sql(
                @"CREATE INDEX ""IX_ProductActivityLogs_CreatedAt_BRIN""
                  ON ""ProductActivityLogs"" USING brin (""CreatedAt"");");

            // ═══════════════════════════════════════════════════════════
            // 5. COVERING INDEXES (INCLUDE) — Index-Only Scan
            // ═══════════════════════════════════════════════════════════

            // Categories: Listeleme sayfasında heap lookup eliminasyonu
            migrationBuilder.Sql(
                @"CREATE INDEX ""IX_Categories_Id_Desc_Cover""
                  ON ""Categories"" (""Id"" DESC)
                  INCLUDE (""Name"", ""IsFavorite"", ""SuperCategoryId"")
                  WHERE NOT ""IsDeleted"";");

            // Products: Ürün listeleme sayfasında heap lookup eliminasyonu
            migrationBuilder.Sql(
                @"CREATE INDEX ""IX_Products_CreatedAt_Cover""
                  ON ""MainProducts"" (""CreatedAt"" DESC, ""UpdatedAt"" DESC)
                  INCLUDE (""Title"", ""StockCode"", ""BrandId"", ""CategoryId"")
                  WHERE NOT ""IsDeleted"";");

            // ═══════════════════════════════════════════════════════════
            // 6. MATERIALIZED VIEW — Kategori Stok Agregasyonu
            // ═══════════════════════════════════════════════════════════

            migrationBuilder.Sql(@"
                CREATE MATERIALIZED VIEW mv_category_summary AS
                SELECT
                    c.""Id"" AS ""CategoryId"",
                    c.""Name"",
                    c.""IsFavorite"",
                    c.""SuperCategoryId"",
                    COUNT(DISTINCT p.""Id"")::int AS ""ProductCount"",
                    COALESCE(SUM(bos.""CurrentStock""), 0)::int AS ""TotalStock"",
                    (SELECT COUNT(*)::int FROM ""Categories"" sub
                     WHERE sub.""SuperCategoryId"" = c.""Id"" AND NOT sub.""IsDeleted"") AS ""SubCategoryCount"",
                    (SELECT COUNT(*)::int FROM ""CategoryAttributeCategories"" cac
                     WHERE cac.""CategoryId"" = c.""Id"") AS ""AttributeCount"",
                    sc.""Name"" AS ""SuperCategoryName""
                FROM ""Categories"" c
                LEFT JOIN ""MainProducts"" p ON p.""CategoryId"" = c.""Id"" AND NOT p.""IsDeleted""
                LEFT JOIN ""ProductVariants"" pv ON pv.""ProductId"" = p.""Id"" AND NOT pv.""IsDeleted""
                LEFT JOIN ""BranchOfficeStocks"" bos ON bos.""ProductVariantId"" = pv.""Id""
                LEFT JOIN ""Categories"" sc ON sc.""Id"" = c.""SuperCategoryId""
                WHERE NOT c.""IsDeleted""
                GROUP BY c.""Id"", c.""Name"", c.""IsFavorite"", c.""SuperCategoryId"", sc.""Name""
                WITH DATA;");

            migrationBuilder.Sql(
                @"CREATE UNIQUE INDEX ON mv_category_summary (""CategoryId"");");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Materialized view
            migrationBuilder.Sql(@"DROP MATERIALIZED VIEW IF EXISTS mv_category_summary;");

            // Covering indexes
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Products_CreatedAt_Cover"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Categories_Id_Desc_Cover"";");

            // BRIN indexes — StockMovements B-tree'yi geri kur
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_ProductActivityLogs_CreatedAt_BRIN"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Logs_CreatedAt_BRIN"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_StockMovements_CreatedAt_BRIN"";");
            migrationBuilder.Sql(
                @"CREATE INDEX ""IX_StockMovements_CreatedAt"" ON ""StockMovements"" (""CreatedAt"");");

            // Recursive CTE fonksiyonu
            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS fn_get_descendant_ids(int);");

            // Trigram indexes + extension
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Categories_Name_Trgm"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_CategoryAttributes_Key_Trgm"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_CategoryAttributes_Humanized_Trgm"";");
            migrationBuilder.Sql(@"DROP EXTENSION IF EXISTS pg_trgm;");

            // Partial indexes — ProductVariants orijinal index'i geri kur
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_CategoryAttributeCategories_CategoryId"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_ProductVariants_Barcode_Active"";");
            migrationBuilder.Sql(
                @"CREATE UNIQUE INDEX ""IX_ProductVariants_Barcode"" ON ""ProductVariants"" (""Barcode"");");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Orders_MarketPlaceId_OrderDate_Active"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Orders_ShipmentPackageId_Active"";");
        }
    }
}

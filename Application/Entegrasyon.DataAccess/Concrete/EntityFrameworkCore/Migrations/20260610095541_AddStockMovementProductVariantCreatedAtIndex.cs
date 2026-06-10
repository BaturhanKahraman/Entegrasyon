using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <summary>
    /// PERF (ürün-360 stok timeline) — StockMovements ProductVariantId + CreatedAt composite index.
    ///
    /// DB Master review (ProductActivityPageManager.GetStockMovementsAsync): sorgu şekli
    /// "WHERE NOT IsDeleted AND ProductVariantId IN (ürünün variant'ları) ORDER BY CreatedAt DESC
    /// LIMIT 100". Mevcut (ProductVariantId, BranchOfficeId) composite filtreyi karşılıyor ama
    /// CreatedAt sırasını veremiyor; planlayıcı ya CreatedAt index'ini geriye tarayıp filtreliyor
    /// (hedef variant seyrek/eski hareketliyse çok index entry okur) ya da filtreli kümeyi sort
    /// ediyor. StockMovements append-only audit, sınırsız büyür.
    ///
    /// Bu partial index (ProductVariantId, CreatedAt DESC) WHERE NOT IsDeleted filter+sort'u tek
    /// index'le karşılar → ürünün variant'larına doğrudan seek + CreatedAt sırası hazır, LIMIT 100
    /// erken durur (Sort/geniş-tarama yok). EXPLAIN ile doğrulandı (RED→GREEN, #6 deseni).
    ///
    /// Raw SQL + IF NOT EXISTS: multi-tenant filoda idempotent. Partial predicate global query
    /// filter (NOT IsDeleted) ile hizalı. EF model'e (snapshot) eklenmez — has-pending temiz kalır.
    /// </summary>
    public partial class AddStockMovementProductVariantCreatedAtIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"CREATE INDEX IF NOT EXISTS ""IX_StockMovements_ProductVariantId_CreatedAt""
                  ON ""StockMovements"" (""ProductVariantId"", ""CreatedAt"" DESC)
                  WHERE NOT ""IsDeleted"";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_StockMovements_ProductVariantId_CreatedAt"";");
        }
    }
}

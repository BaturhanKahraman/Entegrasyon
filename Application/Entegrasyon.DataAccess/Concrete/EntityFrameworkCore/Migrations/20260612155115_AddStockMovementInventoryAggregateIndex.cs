using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <summary>
    /// PERF (Envanter raporu — /reports/inventory) — StockMovements
    /// (BranchOfficeId, ProductVariantId, CreatedAt DESC) WHERE NOT IsDeleted kapsayıcı partial index.
    ///
    /// DB Master review (ReportManager.GetInventoryReportAsync): envanter raporu TÜM stok satırlarını
    /// listeler ve her (şube, varyant) için iki grouped aggregate üretir:
    ///   (a) LastStockEntryDate = MAX("CreatedAt") WHERE Quantity>0 GROUP BY (BranchOfficeId, ProductVariantId)
    ///       — "ürün ne kadar süredir stokta / ölü stok" (bebe giyimde kritik).
    ///   (b) Dönem SoldQuantity = SUM(Quantity) WHERE Type∈(Sale,MarketplaceSale) AND CreatedAt∈[başla,bitir)
    ///       GROUP BY (BranchOfficeId, ProductVariantId) — satılmayan stok (UnsoldOnly) tespiti.
    /// StockAlert raporundaki batched/sayfalı varyant-IN-list deseninin aksine envanter raporu
    /// sayfasız → grouped MAX/SUM TÜM tabloyu tarar. StockMovements append-only audit, sınırsız büyür.
    ///
    /// Mevcut indexler bu grouped aggregate'i karşılamıyor:
    ///   - (ProductVariantId, BranchOfficeId): gruplama anahtarını verir ama CreatedAt taşımaz → MAX için heap.
    ///   - (CreatedAt): dönem range'i için ama gruplama sırası değil.
    ///   - (ProductVariantId, CreatedAt DESC) WHERE NOT IsDeleted: ürün-360 timeline'a özel, BranchOfficeId yok.
    /// Bu yeni composite gruplama anahtarını (BranchOfficeId, ProductVariantId) öne alıp CreatedAt'i
    /// taşıyarak grouped MAX'ı index-tabanlı yapar; partial (WHERE NOT IsDeleted) global query filter ile
    /// hizalı, küçük + bloat-az tutar.
    ///
    /// Raw SQL + IF NOT EXISTS: multi-tenant filoda idempotent. EF model'e (snapshot) eklenmez →
    /// has-pending-model-changes temiz kalır.
    /// </summary>
    public partial class AddStockMovementInventoryAggregateIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"CREATE INDEX IF NOT EXISTS ""IX_StockMovements_Branch_Variant_CreatedAt""
                  ON ""StockMovements"" (""BranchOfficeId"", ""ProductVariantId"", ""CreatedAt"" DESC)
                  WHERE NOT ""IsDeleted"";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_StockMovements_Branch_Variant_CreatedAt"";");
        }
    }
}

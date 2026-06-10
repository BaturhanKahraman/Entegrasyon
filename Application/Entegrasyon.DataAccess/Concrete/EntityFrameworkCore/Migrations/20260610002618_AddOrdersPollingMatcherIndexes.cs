using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <summary>
    /// PERF HIGH-1 (Task #6) — Orders pazaryeri polling matcher + admin listeleme index'leri.
    ///
    /// Denetim + pg_indexes ile dogrulandi: fresh-migrate edilmis Orders tablosunda yalnizca
    /// IX_Orders_MarketPlaceId ve IX_Orders_MarketplaceOrderStatus_Active vardi. Asagidaki
    /// sutunlar index'siz oldugundan polling matcher'lari ve siparis listesi seq scan yapiyordu:
    ///   - OrderNumber       -> N11/Pazarama eslestirme (WHERE OrderNumber = ANY) + GetOrderByNumber
    ///   - ShipmentPackageId -> Trendyol eslestirme (WHERE ShipmentPackageId = ANY)
    ///   - CustomerId        -> Storefront "siparislerim"
    ///   - (MarketPlaceId, OrderDate DESC) -> admin siparis listesi filtre+sort
    ///
    /// Tum index'ler partial (WHERE NOT "IsDeleted") - global query filter ile hizali ve
    /// "= ANY(...)" sorgularinda dogrudan kullanilabilir (EXPLAIN ile dogrulandi). IS NOT NULL
    /// predicate'i BILEREK eklenmedi: planlayici "= ANY" implikasyonunu o predicate ile kanitlayamaz.
    /// IF NOT EXISTS: multi-tenant filoda farkli DB durumlarina karsi idempotent.
    /// </summary>
    public partial class AddOrdersPollingMatcherIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // N11/Pazarama OrderNumber eslestirme + GetOrderByNumber
            migrationBuilder.Sql(
                @"CREATE INDEX IF NOT EXISTS ""IX_Orders_OrderNumber_Active""
                  ON ""Orders"" (""OrderNumber"")
                  WHERE NOT ""IsDeleted"";");

            // Trendyol ShipmentPackageId eslestirme (import deduplication okumasi)
            migrationBuilder.Sql(
                @"CREATE INDEX IF NOT EXISTS ""IX_Orders_ShipmentPackageId_Active""
                  ON ""Orders"" (""ShipmentPackageId"")
                  WHERE NOT ""IsDeleted"";");

            // Storefront B2C "siparislerim"
            migrationBuilder.Sql(
                @"CREATE INDEX IF NOT EXISTS ""IX_Orders_CustomerId_Active""
                  ON ""Orders"" (""CustomerId"")
                  WHERE NOT ""IsDeleted"";");

            // Admin siparis listesi: MarketPlaceId filtre + OrderDate DESC sort
            migrationBuilder.Sql(
                @"CREATE INDEX IF NOT EXISTS ""IX_Orders_MarketPlaceId_OrderDate_Active""
                  ON ""Orders"" (""MarketPlaceId"", ""OrderDate"" DESC)
                  WHERE NOT ""IsDeleted"";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Orders_OrderNumber_Active"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Orders_ShipmentPackageId_Active"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Orders_CustomerId_Active"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Orders_MarketPlaceId_OrderDate_Active"";");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddDatabaseIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ═══════════════════════════════════════════════════════════
            // Öncelik 1 — Products (yüksek trafik)
            // ═══════════════════════════════════════════════════════════

            // StockCode benzersizlik kontrolü — her ürün ekleme/güncelleme'de çalışıyor
            migrationBuilder.Sql(
                @"CREATE UNIQUE INDEX ""IX_Products_StockCode_Active"" ON ""MainProducts"" (""StockCode"") WHERE NOT ""IsDeleted"";");

            // Tarih bazlı sıralama — liste sayfaları
            migrationBuilder.Sql(
                @"CREATE INDEX ""IX_Products_CreatedAt_Desc_Active"" ON ""MainProducts"" (""CreatedAt"" DESC) WHERE NOT ""IsDeleted"";");

            // Marketplace sync karşılaştırması
            migrationBuilder.Sql(
                @"CREATE INDEX ""IX_Products_UpdatedAt_Active"" ON ""MainProducts"" (""UpdatedAt"") WHERE NOT ""IsDeleted"";");

            // ═══════════════════════════════════════════════════════════
            // Öncelik 1 — ProductVariants
            // ═══════════════════════════════════════════════════════════

            // Son eklenen varyant (barkod üretimi)
            migrationBuilder.Sql(
                @"CREATE INDEX ""IX_ProductVariants_CreatedAt_Desc_Active"" ON ""ProductVariants"" (""CreatedAt"" DESC) WHERE NOT ""IsDeleted"";");

            // ═══════════════════════════════════════════════════════════
            // Öncelik 1 — ProductMarketplaces
            // ═══════════════════════════════════════════════════════════

            // Sync summary GroupBy + durum filtresi
            migrationBuilder.Sql(
                @"CREATE INDEX ""IX_ProductMarketplaces_MarketPlaceId_Status_Active"" ON ""ProductMarketplaces"" (""MarketPlaceId"", ""Status"") WHERE NOT ""IsDeleted"";");

            // ═══════════════════════════════════════════════════════════
            // Öncelik 2 — Sales (raporlama)
            // ═══════════════════════════════════════════════════════════

            // Tarih bazlı satış raporları
            migrationBuilder.Sql(
                @"CREATE INDEX ""IX_Sales_CreatedAt_Desc_Active"" ON ""Sales"" (""CreatedAt"" DESC) WHERE NOT ""IsDeleted"";");

            // ═══════════════════════════════════════════════════════════
            // Öncelik 3 — Categories
            // ═══════════════════════════════════════════════════════════

            // Favori sıralama (kategori listesi)
            migrationBuilder.Sql(
                @"CREATE INDEX ""IX_Categories_IsFavorite_Id_Active"" ON ""Categories"" (""IsFavorite"" DESC, ""Id"" DESC) WHERE NOT ""IsDeleted"";");

            // ═══════════════════════════════════════════════════════════
            // Öncelik 4 — Brands
            // ═══════════════════════════════════════════════════════════

            // İsim bazlı benzersizlik kontrolü (soft-delete aware)
            migrationBuilder.Sql(
                @"CREATE UNIQUE INDEX ""IX_Brands_Name_Active"" ON ""Brands"" (""Name"") WHERE NOT ""IsDeleted"";");

            // Tarih sıralama
            migrationBuilder.Sql(
                @"CREATE INDEX ""IX_Brands_CreatedAt_Desc_Active"" ON ""Brands"" (""CreatedAt"" DESC) WHERE NOT ""IsDeleted"";");

            // ═══════════════════════════════════════════════════════════
            // Öncelik 4 — Notifications
            // ═══════════════════════════════════════════════════════════

            // Kullanıcı bildirimleri (join table — FK index eksik)
            migrationBuilder.Sql(
                @"CREATE INDEX ""IX_NotificationsUsers_ApplicationUserId"" ON ""NotificationsUsers"" (""ApplicationUserId"");");

            // Okunmamış bildirim filtresi
            migrationBuilder.Sql(
                @"CREATE INDEX ""IX_Notifications_IsRead_Active"" ON ""Notifications"" (""IsRead"") WHERE NOT ""IsDeleted"";");

            // ═══════════════════════════════════════════════════════════
            // Öncelik 4 — ApplicationLogs (log tipi + aksiyon composite)
            // ═══════════════════════════════════════════════════════════
            // Not: IX_Logs_LogAction ve IX_Logs_LogAction_LogType zaten mevcut — ek index gerekmiyor
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Products_StockCode_Active"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Products_CreatedAt_Desc_Active"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Products_UpdatedAt_Active"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_ProductVariants_CreatedAt_Desc_Active"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_ProductMarketplaces_MarketPlaceId_Status_Active"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Sales_CreatedAt_Desc_Active"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Categories_IsFavorite_Id_Active"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Brands_Name_Active"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Brands_CreatedAt_Desc_Active"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_NotificationsUsers_ApplicationUserId"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Notifications_IsRead_Active"";");
        }
    }
}

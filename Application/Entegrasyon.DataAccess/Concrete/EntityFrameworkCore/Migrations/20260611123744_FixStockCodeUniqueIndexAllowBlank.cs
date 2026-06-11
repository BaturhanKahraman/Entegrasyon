using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class FixStockCodeUniqueIndexAllowBlank : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // BUG: StockCode opsiyonel; boş bırakılınca "" (boş string) yazılıyor.
            // Eski partial unique index ("WHERE NOT IsDeleted") boş string'i değer
            // sayıyordu → ikinci stok-kodsuz ürün 23505 duplicate-key ile patlıyordu.
            // Düzeltme: boş string'i filtre dışına al — gerçek (non-blank) SKU
            // benzersizliği korunur, çok sayıda stok-kodsuz ürün serbest olur.
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Products_StockCode_Active"";");
            migrationBuilder.Sql(
                @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Products_StockCode_Active"" ON ""MainProducts"" (""StockCode"") WHERE NOT ""IsDeleted"" AND ""StockCode"" <> '';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Orijinal (filtresiz) index'e geri dön.
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Products_StockCode_Active"";");
            migrationBuilder.Sql(
                @"CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Products_StockCode_Active"" ON ""MainProducts"" (""StockCode"") WHERE NOT ""IsDeleted"";");
        }
    }
}

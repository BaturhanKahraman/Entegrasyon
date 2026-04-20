using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddPosSearchTrigramIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // pg_trgm extension zaten PostgresOptimizations migration'unda kurulu.
            // POS arama: Title (fuzzy) + StockCode (prefix) + Barcode (ILIKE) icin GIN trigram indexleri.
            migrationBuilder.Sql(@"CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_mainproducts_title_trgm
                ON ""MainProducts"" USING gin (""Title"" gin_trgm_ops);
            ");
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_mainproducts_stockcode_trgm
                ON ""MainProducts"" USING gin (""StockCode"" gin_trgm_ops)
                WHERE ""StockCode"" IS NOT NULL;
            ");
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_productvariants_barcode_trgm
                ON ""ProductVariants"" USING gin (""Barcode"" gin_trgm_ops)
                WHERE ""Barcode"" IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ix_productvariants_barcode_trgm;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ix_mainproducts_stockcode_trgm;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ix_mainproducts_title_trgm;");
        }
    }
}

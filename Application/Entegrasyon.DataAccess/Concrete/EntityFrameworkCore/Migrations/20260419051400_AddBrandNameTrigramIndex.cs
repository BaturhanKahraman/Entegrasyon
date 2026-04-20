using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandNameTrigramIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Global Ctrl+K search: Brand.Name fuzzy/typo-tolerant arama için GIN trigram index.
            // pg_trgm extension PostgresOptimizations migration'unda yüklü.
            migrationBuilder.Sql(@"CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Brands_Name_Trgm""
                ON ""Brands"" USING gin (""Name"" gin_trgm_ops)
                WHERE NOT ""IsDeleted"";
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Brands_Name_Trgm"";");
        }
    }
}

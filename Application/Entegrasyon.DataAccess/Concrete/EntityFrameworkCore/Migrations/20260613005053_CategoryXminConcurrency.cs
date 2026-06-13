using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class CategoryXminConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op: "xmin" PostgreSQL'in her tabloda zaten var olan sistem kolonudur.
            // RowVersion property'si bu sistem kolonuna optimistic-concurrency token olarak
            // map'lenir; fiziksel kolon eklenmez (AddColumn fresh DB'de çakışırdı).
            // Migration yalnızca model-snapshot senkronizasyonu için tutulur.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op — sistem kolonu kaldırılamaz/eklenmez.
        }
    }
}

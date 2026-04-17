using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandNormalizedNameAndFilteredIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Brands_Name",
                table: "Brands");

            migrationBuilder.DropIndex(
                name: "IX_Brands_SeoSlug",
                table: "Brands");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "Brands",
                type: "character varying(55)",
                maxLength: 55,
                nullable: true);

            // Backfill: mevcut markaların NormalizedName ve SeoSlug alanlarını doldur
            migrationBuilder.Sql("""
                UPDATE "Brands" SET
                    "NormalizedName" = UPPER(TRIM("Name")),
                    "SeoSlug" = LOWER(REGEXP_REPLACE(TRIM("Name"), '[^a-zA-Z0-9]+', '-', 'g'))
                WHERE "IsDeleted" = false;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Brands_NormalizedName",
                table: "Brands",
                column: "NormalizedName",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Brands_SeoSlug",
                table: "Brands",
                column: "SeoSlug",
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Brands_NormalizedName",
                table: "Brands");

            migrationBuilder.DropIndex(
                name: "IX_Brands_SeoSlug",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "Brands");

            migrationBuilder.CreateIndex(
                name: "IX_Brands_Name",
                table: "Brands",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Brands_SeoSlug",
                table: "Brands",
                column: "SeoSlug",
                unique: true,
                filter: "\"SeoSlug\" IS NOT NULL");
        }
    }
}

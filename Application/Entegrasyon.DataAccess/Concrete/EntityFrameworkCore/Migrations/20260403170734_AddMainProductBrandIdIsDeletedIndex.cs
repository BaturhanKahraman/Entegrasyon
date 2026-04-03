using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddMainProductBrandIdIsDeletedIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MainProducts_BrandId",
                table: "MainProducts");

            migrationBuilder.CreateIndex(
                name: "IX_MainProducts_BrandId_IsDeleted",
                table: "MainProducts",
                columns: new[] { "BrandId", "IsDeleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MainProducts_BrandId_IsDeleted",
                table: "MainProducts");

            migrationBuilder.CreateIndex(
                name: "IX_MainProducts_BrandId",
                table: "MainProducts",
                column: "BrandId");
        }
    }
}

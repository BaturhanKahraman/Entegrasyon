using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddKdvFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "VatRate",
                table: "OrderItems",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultVatRate",
                table: "Categories",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VatRate",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "DefaultVatRate",
                table: "Categories");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class SeedN11MarketPlace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "MarketPlaces",
                columns: new[] { "Id", "Name", "IsDeleted", "CreatedAt" },
                values: new object[] { 2, "N11", false, new DateTime(2026, 3, 22, 0, 0, 0, DateTimeKind.Utc) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MarketPlaces",
                keyColumn: "Id",
                keyValue: 2);
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class SeedPazaramaMarketPlace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "MarketPlaces",
                columns: new[] { "Id", "Name", "BaseUrl", "TokenUrl", "IsDeleted", "CreatedAt" },
                values: new object[] {
                    4, "Pazarama",
                    "https://isortagimapi.pazarama.com",
                    "https://isortagimgiris.pazarama.com/connect/token",
                    false, new DateTime(2026, 3, 22, 0, 0, 0, DateTimeKind.Utc)
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MarketPlaces",
                keyColumn: "Id",
                keyValue: 4);
        }
    }
}

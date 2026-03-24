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
                columns: new[] { "Id", "Name", "BaseUrl", "TokenUrl", "IsBasicAuth", "IsDeleted", "CreatedAt", "DeletedAt", "UpdatedAt" },
                values: new object[] {
                    5, "Pazarama",
                    "https://isortagimapi.pazarama.com",
                    "https://isortagimgiris.pazarama.com/connect/token",
                    false, false,
                    new DateTimeOffset(new DateTime(2026, 3, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                    new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                    new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0))
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MarketPlaces",
                keyColumn: "Id",
                keyValue: 5);
        }
    }
}

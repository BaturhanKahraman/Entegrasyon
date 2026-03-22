using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddAmazonMarketPlaceAndRefreshToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RefreshToken",
                table: "MarketPlaces",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "MarketPlaces",
                keyColumn: "Id",
                keyValue: 1,
                column: "RefreshToken",
                value: null);

            migrationBuilder.InsertData(
                table: "MarketPlaces",
                columns: new[] { "Id", "Name", "IsBasicAuth", "IsDeleted", "CreatedAt",
                    "BaseUrl", "TokenUrl" },
                values: new object[] { 6, "Amazon", false, false,
                    new DateTime(2026, 3, 22, 0, 0, 0, DateTimeKind.Utc),
                    "https://sellingpartnerapi-eu.amazon.com",
                    "https://api.amazon.com/auth/o2/token" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MarketPlaces",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DropColumn(
                name: "RefreshToken",
                table: "MarketPlaces");
        }
    }
}

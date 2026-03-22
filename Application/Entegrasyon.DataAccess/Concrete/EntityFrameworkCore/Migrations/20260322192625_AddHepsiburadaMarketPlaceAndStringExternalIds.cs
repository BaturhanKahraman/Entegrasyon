using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddHepsiburadaMarketPlaceAndStringExternalIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MarketPlaceCategoryAttributeValueExternalId",
                table: "CategoryAttributeValueMarketPlaceMatches",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MarketPlaceCategoryAttributeExternalId",
                table: "CategoryAttributeMarketPlaceMatches",
                type: "text",
                nullable: true);

            migrationBuilder.InsertData(
                table: "MarketPlaces",
                columns: new[] { "Id", "Name", "IsBasicAuth", "IsDeleted", "CreatedAt" },
                values: new object[] { 3, "Hepsiburada", true, false, new DateTime(2026, 3, 22, 0, 0, 0, DateTimeKind.Utc) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MarketPlaces",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DropColumn(
                name: "MarketPlaceCategoryAttributeValueExternalId",
                table: "CategoryAttributeValueMarketPlaceMatches");

            migrationBuilder.DropColumn(
                name: "MarketPlaceCategoryAttributeExternalId",
                table: "CategoryAttributeMarketPlaceMatches");
        }
    }
}

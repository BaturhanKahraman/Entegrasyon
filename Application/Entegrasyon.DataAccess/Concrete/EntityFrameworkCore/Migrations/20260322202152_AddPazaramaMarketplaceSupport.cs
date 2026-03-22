using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddPazaramaMarketplaceSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TokenUrl",
                table: "MarketPlaces",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MarketPlaceBrandExternalId",
                table: "BrandMarketPlaceMatches",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "MarketPlaces",
                keyColumn: "Id",
                keyValue: 1,
                column: "TokenUrl",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TokenUrl",
                table: "MarketPlaces");

            migrationBuilder.DropColumn(
                name: "MarketPlaceBrandExternalId",
                table: "BrandMarketPlaceMatches");
        }
    }
}

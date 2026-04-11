using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandNameToMarketPlaceMatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MarketPlaceBrandName",
                table: "BrandMarketPlaceMatches",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MarketPlaceBrandName",
                table: "BrandMarketPlaceMatches");
        }
    }
}

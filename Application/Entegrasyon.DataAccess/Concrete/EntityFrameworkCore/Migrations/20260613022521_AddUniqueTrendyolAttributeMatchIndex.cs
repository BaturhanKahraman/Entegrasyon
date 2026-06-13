using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueTrendyolAttributeMatchIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_CategoryAttributeMarketPlaceMatches_MarketPlaceId_MarketPla~",
                table: "CategoryAttributeMarketPlaceMatches",
                columns: new[] { "MarketPlaceId", "MarketPlaceCategoryAttributeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CategoryAttributeMarketPlaceMatches_MarketPlaceId_MarketPla~",
                table: "CategoryAttributeMarketPlaceMatches");
        }
    }
}

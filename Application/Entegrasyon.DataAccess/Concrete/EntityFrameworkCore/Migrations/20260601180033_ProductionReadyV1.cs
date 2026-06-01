using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class ProductionReadyV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // defaultValue mevcut satırlara uygulanır; entity C# initializer'larıyla (500/5000/2000)
            // hizalı tutuldu ki migration sonrası eski kayıtlar 0 yerine anlamlı eşik alsın.
            migrationBuilder.AddColumn<int>(
                name: "LoyaltyBirthdayBonus",
                table: "StorefrontSettings",
                type: "integer",
                nullable: false,
                defaultValue: 500);

            migrationBuilder.AddColumn<int>(
                name: "LoyaltyTierGoldMin",
                table: "StorefrontSettings",
                type: "integer",
                nullable: false,
                defaultValue: 5000);

            migrationBuilder.AddColumn<int>(
                name: "LoyaltyTierSilverMin",
                table: "StorefrontSettings",
                type: "integer",
                nullable: false,
                defaultValue: 2000);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LoyaltyBirthdayBonus",
                table: "StorefrontSettings");

            migrationBuilder.DropColumn(
                name: "LoyaltyTierGoldMin",
                table: "StorefrontSettings");

            migrationBuilder.DropColumn(
                name: "LoyaltyTierSilverMin",
                table: "StorefrontSettings");
        }
    }
}

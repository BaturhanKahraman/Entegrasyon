using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscountVoucherEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentUsageCount",
                table: "DiscountVouchers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DiscountType",
                table: "DiscountVouchers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxUsageCount",
                table: "DiscountVouchers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinimumCartAmount",
                table: "DiscountVouchers",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentUsageCount",
                table: "DiscountVouchers");

            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "DiscountVouchers");

            migrationBuilder.DropColumn(
                name: "MaxUsageCount",
                table: "DiscountVouchers");

            migrationBuilder.DropColumn(
                name: "MinimumCartAmount",
                table: "DiscountVouchers");
        }
    }
}

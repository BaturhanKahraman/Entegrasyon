using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleGeneralDiscountAndReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "GeneralDiscount",
                table: "Sales",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AddColumn<int>(
                name: "GeneralDiscountReasonId",
                table: "Sales",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeneralDiscountReasonNote",
                table: "Sales",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sales_GeneralDiscountReasonId",
                table: "Sales",
                column: "GeneralDiscountReasonId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_DiscountReasons_GeneralDiscountReasonId",
                table: "Sales",
                column: "GeneralDiscountReasonId",
                principalTable: "DiscountReasons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sales_DiscountReasons_GeneralDiscountReasonId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_GeneralDiscountReasonId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "GeneralDiscountReasonId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "GeneralDiscountReasonNote",
                table: "Sales");

            migrationBuilder.AlterColumn<double>(
                name: "GeneralDiscount",
                table: "Sales",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");
        }
    }
}

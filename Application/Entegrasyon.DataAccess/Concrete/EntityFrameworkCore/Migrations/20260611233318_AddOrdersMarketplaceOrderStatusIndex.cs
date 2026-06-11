using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddOrdersMarketplaceOrderStatusIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Orders_MarketplaceOrderStatus_OrderDate",
                table: "Orders",
                columns: new[] { "MarketplaceOrderStatus", "OrderDate" },
                filter: "\"MarketplaceOrderStatus\" IS NOT NULL AND NOT \"IsDeleted\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_MarketplaceOrderStatus_OrderDate",
                table: "Orders");
        }
    }
}

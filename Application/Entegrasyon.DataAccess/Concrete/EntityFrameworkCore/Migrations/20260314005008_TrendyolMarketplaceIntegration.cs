using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class TrendyolMarketplaceIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ContentId",
                table: "ProductMarketplaces",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "ProductMarketplaces",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "ProductMarketplaces",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CargoProviderName",
                table: "Orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CargoTrackingLink",
                table: "Orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CargoTrackingNumber",
                table: "Orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerEmail",
                table: "Orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerFirstName",
                table: "Orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerLastName",
                table: "Orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EstimatedDeliveryEndDate",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GrossAmount",
                table: "Orders",
                type: "money",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFastDelivery",
                table: "Orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsMicro",
                table: "Orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MarketPlaceId",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MarketplaceOrderStatus",
                table: "Orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "OrderDate",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrderNumber",
                table: "Orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ShipmentPackageId",
                table: "Orders",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "OrderItems",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Discount",
                table: "OrderItems",
                type: "money",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LineId",
                table: "OrderItems",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MerchantSku",
                table: "OrderItems",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductColor",
                table: "OrderItems",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductSize",
                table: "OrderItems",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BaseUrl",
                table: "MarketPlaces",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerId",
                table: "MarketPlaces",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserAgentPrefix",
                table: "MarketPlaces",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MarketPlaceWarehouses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MarketPlaceId = table.Column<int>(type: "integer", nullable: false),
                    BranchOfficeId = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketPlaceWarehouses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketPlaceWarehouses_BranchOffices_BranchOfficeId",
                        column: x => x.BranchOfficeId,
                        principalTable: "BranchOffices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MarketPlaceWarehouses_MarketPlaces_MarketPlaceId",
                        column: x => x.MarketPlaceId,
                        principalTable: "MarketPlaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "MarketPlaces",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "BaseUrl", "SellerId", "UserAgentPrefix" },
                values: new object[] { null, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_MarketPlaceId",
                table: "Orders",
                column: "MarketPlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketPlaceWarehouses_BranchOfficeId",
                table: "MarketPlaceWarehouses",
                column: "BranchOfficeId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketPlaceWarehouses_MarketPlaceId_BranchOfficeId",
                table: "MarketPlaceWarehouses",
                columns: new[] { "MarketPlaceId", "BranchOfficeId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_MarketPlaces_MarketPlaceId",
                table: "Orders",
                column: "MarketPlaceId",
                principalTable: "MarketPlaces",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_MarketPlaces_MarketPlaceId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "MarketPlaceWarehouses");

            migrationBuilder.DropIndex(
                name: "IX_Orders_MarketPlaceId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ContentId",
                table: "ProductMarketplaces");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "ProductMarketplaces");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "ProductMarketplaces");

            migrationBuilder.DropColumn(
                name: "CargoProviderName",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CargoTrackingLink",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CargoTrackingNumber",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CustomerEmail",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CustomerFirstName",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CustomerLastName",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "EstimatedDeliveryEndDate",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "GrossAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsFastDelivery",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsMicro",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "MarketPlaceId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "MarketplaceOrderStatus",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OrderDate",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OrderNumber",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShipmentPackageId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "Discount",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "LineId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "MerchantSku",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ProductColor",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ProductSize",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "BaseUrl",
                table: "MarketPlaces");

            migrationBuilder.DropColumn(
                name: "SellerId",
                table: "MarketPlaces");

            migrationBuilder.DropColumn(
                name: "UserAgentPrefix",
                table: "MarketPlaces");
        }
    }
}

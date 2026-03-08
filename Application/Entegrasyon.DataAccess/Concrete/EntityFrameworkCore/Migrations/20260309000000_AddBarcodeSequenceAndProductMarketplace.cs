using System;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    [DbContext(typeof(IntegrationDbContext))]
    [Migration("20260309000000_AddBarcodeSequenceAndProductMarketplace")]
    public partial class AddBarcodeSequenceAndProductMarketplace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop old TempBarcodes table
            migrationBuilder.DropTable(name: "TempBarcodes");

            // Create atomic barcode sequence
            migrationBuilder.Sql("CREATE SEQUENCE barcode_sequence START WITH 1000000000000 INCREMENT BY 1 NO CYCLE;");

            // Create ProductMarketplaces table
            migrationBuilder.CreateTable(
                name: "ProductMarketplaces",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    MarketPlaceId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    BatchRequestId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ExternalProductId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StatusMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LastSyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductMarketplaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductMarketplaces_MainProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "MainProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductMarketplaces_MarketPlaces_MarketPlaceId",
                        column: x => x.MarketPlaceId,
                        principalTable: "MarketPlaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductMarketplaces_MarketPlaceId",
                table: "ProductMarketplaces",
                column: "MarketPlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductMarketplaces_ProductId_MarketPlaceId",
                table: "ProductMarketplaces",
                columns: new[] { "ProductId", "MarketPlaceId" },
                unique: true);

            // Fix money column precision
            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "OrderItems",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "SaleItems",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "DiscountVouchers",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ProductMarketplaces");

            migrationBuilder.Sql("DROP SEQUENCE IF EXISTS barcode_sequence;");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "OrderItems",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "SaleItems",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "DiscountVouchers",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketplaceOverrides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DescriptionOverride",
                table: "ProductMarketplaces",
                type: "character varying(30000)",
                maxLength: 30000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TitleOverride",
                table: "ProductMarketplaces",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductVariantMarketplaceOverrides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductMarketplaceId = table.Column<int>(type: "integer", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ListPriceOverride = table.Column<decimal>(type: "money", nullable: true),
                    SalePriceOverride = table.Column<decimal>(type: "money", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariantMarketplaceOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariantMarketplaceOverrides_ProductMarketplaces_Prod~",
                        column: x => x.ProductMarketplaceId,
                        principalTable: "ProductMarketplaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductVariantMarketplaceOverrides_ProductVariants_ProductV~",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantMarketplaceOverrides_ProductMarketplaceId_Pro~",
                table: "ProductVariantMarketplaceOverrides",
                columns: new[] { "ProductMarketplaceId", "ProductVariantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantMarketplaceOverrides_ProductVariantId",
                table: "ProductVariantMarketplaceOverrides",
                column: "ProductVariantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductVariantMarketplaceOverrides");

            migrationBuilder.DropColumn(
                name: "DescriptionOverride",
                table: "ProductMarketplaces");

            migrationBuilder.DropColumn(
                name: "TitleOverride",
                table: "ProductMarketplaces");
        }
    }
}

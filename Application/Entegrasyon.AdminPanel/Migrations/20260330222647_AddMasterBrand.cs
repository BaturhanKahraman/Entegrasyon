using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Entegrasyon.AdminPanel.Migrations
{
    /// <inheritdoc />
    public partial class AddMasterBrand : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MasterBrands",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterBrands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MasterBrandMarketplaceMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MasterBrandId = table.Column<int>(type: "integer", nullable: false),
                    MarketplaceId = table.Column<int>(type: "integer", nullable: false),
                    ExternalBrandId = table.Column<int>(type: "integer", nullable: false),
                    ExternalBrandName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterBrandMarketplaceMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MasterBrandMarketplaceMappings_MasterBrands_MasterBrandId",
                        column: x => x.MasterBrandId,
                        principalTable: "MasterBrands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MasterBrandMarketplaceMappings_MasterBrandId_MarketplaceId",
                table: "MasterBrandMarketplaceMappings",
                columns: new[] { "MasterBrandId", "MarketplaceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MasterBrands_Name",
                table: "MasterBrands",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MasterBrandMarketplaceMappings");

            migrationBuilder.DropTable(
                name: "MasterBrands");
        }
    }
}

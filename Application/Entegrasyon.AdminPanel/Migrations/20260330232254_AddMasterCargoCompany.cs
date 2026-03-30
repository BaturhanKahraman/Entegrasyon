using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Entegrasyon.AdminPanel.Migrations
{
    /// <inheritdoc />
    public partial class AddMasterCargoCompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MasterCargoCompanies",
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
                    table.PrimaryKey("PK_MasterCargoCompanies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MasterCargoCompanyMarketplaceMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MasterCargoCompanyId = table.Column<int>(type: "integer", nullable: false),
                    MarketplaceId = table.Column<int>(type: "integer", nullable: false),
                    ExternalCargoCompanyId = table.Column<int>(type: "integer", nullable: false),
                    ExternalCargoCompanyName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterCargoCompanyMarketplaceMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MasterCargoCompanyMarketplaceMappings_MasterCargoCompanies_~",
                        column: x => x.MasterCargoCompanyId,
                        principalTable: "MasterCargoCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MasterCargoCompanies_Name",
                table: "MasterCargoCompanies",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MasterCargoCompanyMarketplaceMappings_MasterCargoCompanyId_~",
                table: "MasterCargoCompanyMarketplaceMappings",
                columns: new[] { "MasterCargoCompanyId", "MarketplaceId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MasterCargoCompanyMarketplaceMappings");

            migrationBuilder.DropTable(
                name: "MasterCargoCompanies");
        }
    }
}

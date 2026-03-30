using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Entegrasyon.AdminPanel.Migrations
{
    /// <inheritdoc />
    public partial class AddMasterCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarketplaceReferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MarketplaceId = table.Column<int>(type: "integer", nullable: false),
                    EntityType = table.Column<int>(type: "integer", nullable: false),
                    ExternalId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ParentExternalId = table.Column<string>(type: "text", nullable: true),
                    RawJson = table.Column<string>(type: "text", nullable: true),
                    LastSyncedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketplaceReferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MasterAttributes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "text", nullable: false),
                    HumanizedName = table.Column<string>(type: "text", nullable: false),
                    AllowCustom = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterAttributes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MasterCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ParentId = table.Column<int>(type: "integer", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsLeaf = table.Column<bool>(type: "boolean", nullable: false),
                    OriginalMarketplaceId = table.Column<int>(type: "integer", nullable: true),
                    OriginalExternalId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MasterCategories_MasterCategories_ParentId",
                        column: x => x.ParentId,
                        principalTable: "MasterCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SectorPackages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IconName = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SectorPackages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MasterAttributeMarketplaceMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MasterAttributeId = table.Column<int>(type: "integer", nullable: false),
                    MarketplaceId = table.Column<int>(type: "integer", nullable: false),
                    ExternalAttributeId = table.Column<string>(type: "text", nullable: false),
                    ExternalAttributeName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterAttributeMarketplaceMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MasterAttributeMarketplaceMappings_MasterAttributes_MasterA~",
                        column: x => x.MasterAttributeId,
                        principalTable: "MasterAttributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MasterAttributeValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MasterAttributeId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterAttributeValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MasterAttributeValues_MasterAttributes_MasterAttributeId",
                        column: x => x.MasterAttributeId,
                        principalTable: "MasterAttributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MasterCategoryAttributes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MasterCategoryId = table.Column<int>(type: "integer", nullable: false),
                    MasterAttributeId = table.Column<int>(type: "integer", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    IsVarianter = table.Column<bool>(type: "boolean", nullable: false),
                    IsSlicer = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterCategoryAttributes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MasterCategoryAttributes_MasterAttributes_MasterAttributeId",
                        column: x => x.MasterAttributeId,
                        principalTable: "MasterAttributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MasterCategoryAttributes_MasterCategories_MasterCategoryId",
                        column: x => x.MasterCategoryId,
                        principalTable: "MasterCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MasterCategoryMarketplaceMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MasterCategoryId = table.Column<int>(type: "integer", nullable: false),
                    MarketplaceId = table.Column<int>(type: "integer", nullable: false),
                    ExternalCategoryId = table.Column<string>(type: "text", nullable: false),
                    ExternalCategoryName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterCategoryMarketplaceMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MasterCategoryMarketplaceMappings_MasterCategories_MasterCa~",
                        column: x => x.MasterCategoryId,
                        principalTable: "MasterCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SectorPackageCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SectorPackageId = table.Column<int>(type: "integer", nullable: false),
                    MasterCategoryId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SectorPackageCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SectorPackageCategories_MasterCategories_MasterCategoryId",
                        column: x => x.MasterCategoryId,
                        principalTable: "MasterCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SectorPackageCategories_SectorPackages_SectorPackageId",
                        column: x => x.SectorPackageId,
                        principalTable: "SectorPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MasterValueMarketplaceMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MasterAttributeValueId = table.Column<int>(type: "integer", nullable: false),
                    MarketplaceId = table.Column<int>(type: "integer", nullable: false),
                    ExternalValueId = table.Column<string>(type: "text", nullable: false),
                    ExternalValueName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterValueMarketplaceMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MasterValueMarketplaceMappings_MasterAttributeValues_Master~",
                        column: x => x.MasterAttributeValueId,
                        principalTable: "MasterAttributeValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceReferences_MarketplaceId_EntityType_ExternalId",
                table: "MarketplaceReferences",
                columns: new[] { "MarketplaceId", "EntityType", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MasterAttributeMarketplaceMappings_MasterAttributeId_Market~",
                table: "MasterAttributeMarketplaceMappings",
                columns: new[] { "MasterAttributeId", "MarketplaceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MasterAttributeValues_MasterAttributeId",
                table: "MasterAttributeValues",
                column: "MasterAttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_MasterCategories_ParentId",
                table: "MasterCategories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_MasterCategoryAttributes_MasterAttributeId",
                table: "MasterCategoryAttributes",
                column: "MasterAttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_MasterCategoryAttributes_MasterCategoryId_MasterAttributeId",
                table: "MasterCategoryAttributes",
                columns: new[] { "MasterCategoryId", "MasterAttributeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MasterCategoryMarketplaceMappings_MasterCategoryId_Marketpl~",
                table: "MasterCategoryMarketplaceMappings",
                columns: new[] { "MasterCategoryId", "MarketplaceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MasterValueMarketplaceMappings_MasterAttributeValueId_Marke~",
                table: "MasterValueMarketplaceMappings",
                columns: new[] { "MasterAttributeValueId", "MarketplaceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SectorPackageCategories_MasterCategoryId",
                table: "SectorPackageCategories",
                column: "MasterCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SectorPackageCategories_SectorPackageId",
                table: "SectorPackageCategories",
                column: "SectorPackageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketplaceReferences");

            migrationBuilder.DropTable(
                name: "MasterAttributeMarketplaceMappings");

            migrationBuilder.DropTable(
                name: "MasterCategoryAttributes");

            migrationBuilder.DropTable(
                name: "MasterCategoryMarketplaceMappings");

            migrationBuilder.DropTable(
                name: "MasterValueMarketplaceMappings");

            migrationBuilder.DropTable(
                name: "SectorPackageCategories");

            migrationBuilder.DropTable(
                name: "MasterAttributeValues");

            migrationBuilder.DropTable(
                name: "MasterCategories");

            migrationBuilder.DropTable(
                name: "SectorPackages");

            migrationBuilder.DropTable(
                name: "MasterAttributes");
        }
    }
}

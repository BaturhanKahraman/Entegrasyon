using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchedEntityTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MatchedEntityPackages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    EntityType = table.Column<int>(type: "integer", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchedEntityPackages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TemplateBrandData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PackageId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateBrandData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateBrandData_MatchedEntityPackages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "MatchedEntityPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TemplateCargoCompanyData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PackageId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateCargoCompanyData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateCargoCompanyData_MatchedEntityPackages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "MatchedEntityPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TemplateCategoryData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PackageId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ParentTemplateCategoryDataId = table.Column<int>(type: "integer", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    DefaultVatRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateCategoryData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateCategoryData_MatchedEntityPackages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "MatchedEntityPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TemplateCategoryData_TemplateCategoryData_ParentTemplateCat~",
                        column: x => x.ParentTemplateCategoryDataId,
                        principalTable: "TemplateCategoryData",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TemplateBrandMarketplaceMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TemplateBrandDataId = table.Column<int>(type: "integer", nullable: false),
                    MarketPlaceId = table.Column<int>(type: "integer", nullable: false),
                    ExternalBrandId = table.Column<int>(type: "integer", nullable: false),
                    ExternalBrandExternalId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateBrandMarketplaceMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateBrandMarketplaceMappings_TemplateBrandData_Template~",
                        column: x => x.TemplateBrandDataId,
                        principalTable: "TemplateBrandData",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TemplateCargoCompanyMarketplaceMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TemplateCargoCompanyDataId = table.Column<int>(type: "integer", nullable: false),
                    MarketPlaceId = table.Column<int>(type: "integer", nullable: false),
                    ExternalCargoCompanyId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateCargoCompanyMarketplaceMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateCargoCompanyMarketplaceMappings_TemplateCargoCompan~",
                        column: x => x.TemplateCargoCompanyDataId,
                        principalTable: "TemplateCargoCompanyData",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TemplateCategoryAttributeData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TemplateCategoryDataId = table.Column<int>(type: "integer", nullable: false),
                    AttributeKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AttributeHumanized = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AllowCustom = table.Column<bool>(type: "boolean", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    IsSlicer = table.Column<bool>(type: "boolean", nullable: false),
                    IsVarianter = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateCategoryAttributeData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateCategoryAttributeData_TemplateCategoryData_Template~",
                        column: x => x.TemplateCategoryDataId,
                        principalTable: "TemplateCategoryData",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TemplateCategoryMarketplaceMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TemplateCategoryDataId = table.Column<int>(type: "integer", nullable: false),
                    MarketPlaceId = table.Column<int>(type: "integer", nullable: false),
                    ExternalCategoryId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ExternalCategoryName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateCategoryMarketplaceMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateCategoryMarketplaceMappings_TemplateCategoryData_Te~",
                        column: x => x.TemplateCategoryDataId,
                        principalTable: "TemplateCategoryData",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TemplateCategoryAttributeValueData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TemplateCategoryAttributeDataId = table.Column<int>(type: "integer", nullable: false),
                    ValueName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateCategoryAttributeValueData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateCategoryAttributeValueData_TemplateCategoryAttribut~",
                        column: x => x.TemplateCategoryAttributeDataId,
                        principalTable: "TemplateCategoryAttributeData",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TemplateCategoryAttrMarketplaceMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TemplateCategoryAttributeDataId = table.Column<int>(type: "integer", nullable: false),
                    MarketPlaceId = table.Column<int>(type: "integer", nullable: false),
                    ExternalAttributeId = table.Column<int>(type: "integer", nullable: false),
                    ExternalAttributeExternalId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateCategoryAttrMarketplaceMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateCategoryAttrMarketplaceMappings_TemplateCategoryAtt~",
                        column: x => x.TemplateCategoryAttributeDataId,
                        principalTable: "TemplateCategoryAttributeData",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TemplateCategoryAttrValueMarketplaceMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TemplateCategoryAttributeValueDataId = table.Column<int>(type: "integer", nullable: false),
                    MarketPlaceId = table.Column<int>(type: "integer", nullable: false),
                    ExternalValueId = table.Column<int>(type: "integer", nullable: false),
                    ExternalValueExternalId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateCategoryAttrValueMarketplaceMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateCategoryAttrValueMarketplaceMappings_TemplateCatego~",
                        column: x => x.TemplateCategoryAttributeValueDataId,
                        principalTable: "TemplateCategoryAttributeValueData",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateBrandData_PackageId",
                table: "TemplateBrandData",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateBrandMarketplaceMappings_TemplateBrandDataId",
                table: "TemplateBrandMarketplaceMappings",
                column: "TemplateBrandDataId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateCargoCompanyData_PackageId",
                table: "TemplateCargoCompanyData",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateCargoCompanyMarketplaceMappings_TemplateCargoCompan~",
                table: "TemplateCargoCompanyMarketplaceMappings",
                column: "TemplateCargoCompanyDataId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateCategoryAttributeData_TemplateCategoryDataId",
                table: "TemplateCategoryAttributeData",
                column: "TemplateCategoryDataId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateCategoryAttributeValueData_TemplateCategoryAttribut~",
                table: "TemplateCategoryAttributeValueData",
                column: "TemplateCategoryAttributeDataId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateCategoryAttrMarketplaceMappings_TemplateCategoryAtt~",
                table: "TemplateCategoryAttrMarketplaceMappings",
                column: "TemplateCategoryAttributeDataId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateCategoryAttrValueMarketplaceMappings_TemplateCatego~",
                table: "TemplateCategoryAttrValueMarketplaceMappings",
                column: "TemplateCategoryAttributeValueDataId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateCategoryData_PackageId",
                table: "TemplateCategoryData",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateCategoryData_ParentTemplateCategoryDataId",
                table: "TemplateCategoryData",
                column: "ParentTemplateCategoryDataId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateCategoryMarketplaceMappings_TemplateCategoryDataId",
                table: "TemplateCategoryMarketplaceMappings",
                column: "TemplateCategoryDataId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TemplateBrandMarketplaceMappings");

            migrationBuilder.DropTable(
                name: "TemplateCargoCompanyMarketplaceMappings");

            migrationBuilder.DropTable(
                name: "TemplateCategoryAttrMarketplaceMappings");

            migrationBuilder.DropTable(
                name: "TemplateCategoryAttrValueMarketplaceMappings");

            migrationBuilder.DropTable(
                name: "TemplateCategoryMarketplaceMappings");

            migrationBuilder.DropTable(
                name: "TemplateBrandData");

            migrationBuilder.DropTable(
                name: "TemplateCargoCompanyData");

            migrationBuilder.DropTable(
                name: "TemplateCategoryAttributeValueData");

            migrationBuilder.DropTable(
                name: "TemplateCategoryAttributeData");

            migrationBuilder.DropTable(
                name: "TemplateCategoryData");

            migrationBuilder.DropTable(
                name: "MatchedEntityPackages");
        }
    }
}

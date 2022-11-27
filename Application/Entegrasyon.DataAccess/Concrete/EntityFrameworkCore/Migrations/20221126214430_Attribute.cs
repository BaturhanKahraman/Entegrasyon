using System;
using Entegrasyon.Entity.Categories;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    public partial class Attribute : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttributeKeyValues",
                table: "ProductVariants");

            migrationBuilder.CreateTable(
                name: "AttributeKeyValues",
                columns: table => new
                {
                    CategoryAttributeId = table.Column<int>(type: "integer", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomValue = table.Column<string>(type: "text", nullable: true),
                    AttributeValueId = table.Column<int>(type: "integer", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttributeKeyValues", x => new { x.CategoryAttributeId, x.ProductVariantId });
                    table.ForeignKey(
                        name: "FK_AttributeKeyValues_CategoryAttributes_CategoryAttributeId",
                        column: x => x.CategoryAttributeId,
                        principalTable: "CategoryAttributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttributeKeyValues_CategoryAttributeValues_AttributeValueId",
                        column: x => x.AttributeValueId,
                        principalTable: "CategoryAttributeValues",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AttributeKeyValues_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttributeKeyValues_AttributeValueId",
                table: "AttributeKeyValues",
                column: "AttributeValueId");

            migrationBuilder.CreateIndex(
                name: "IX_AttributeKeyValues_ProductVariantId",
                table: "AttributeKeyValues",
                column: "ProductVariantId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttributeKeyValues");

            migrationBuilder.AddColumn<AttributeKeyValue[]>(
                name: "AttributeKeyValues",
                table: "ProductVariants",
                type: "jsonb",
                nullable: true);
        }
    }
}

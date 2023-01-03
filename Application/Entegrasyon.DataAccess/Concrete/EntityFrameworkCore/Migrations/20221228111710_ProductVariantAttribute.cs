using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    public partial class ProductVariantAttribute : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductVariants_CategoryAttributeValues_CategoryAttributeVa~",
                table: "ProductVariants");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductVariants_ProductVariants_ProductVariantId",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_CategoryAttributeValueId",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_ProductVariantId",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "CategoryAttributeValueId",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "CustomValue",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "IsSlicer",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "IsVarianter",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "ProductVariantId",
                table: "ProductVariants");

            migrationBuilder.AddColumn<string>(
                name: "ProductVariantAttributes",
                table: "ProductVariants",
                type: "jsonb",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductVariantAttributes",
                table: "ProductVariants");

            migrationBuilder.AddColumn<int>(
                name: "CategoryAttributeValueId",
                table: "ProductVariants",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomValue",
                table: "ProductVariants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSlicer",
                table: "ProductVariants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsVarianter",
                table: "ProductVariants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductVariantId",
                table: "ProductVariants",
                type: "uuid",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "ProductVariants",
                keyColumn: "Id",
                keyValue: new Guid("32bfc865-b803-4945-9b1c-9e313a9c6398"),
                column: "CustomValue",
                value: "Kırmızı");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_CategoryAttributeValueId",
                table: "ProductVariants",
                column: "CategoryAttributeValueId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductVariantId",
                table: "ProductVariants",
                column: "ProductVariantId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductVariants_CategoryAttributeValues_CategoryAttributeVa~",
                table: "ProductVariants",
                column: "CategoryAttributeValueId",
                principalTable: "CategoryAttributeValues",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductVariants_ProductVariants_ProductVariantId",
                table: "ProductVariants",
                column: "ProductVariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id");
        }
    }
}

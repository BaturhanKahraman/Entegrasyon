using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    public partial class CheckChange : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CategoryCategoryAttribute_Categories_CategoryId",
                table: "CategoryCategoryAttribute");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CategoryCategoryAttribute",
                table: "CategoryCategoryAttribute");

            migrationBuilder.DropIndex(
                name: "IX_CategoryCategoryAttribute_CategoryId",
                table: "CategoryCategoryAttribute");

            migrationBuilder.RenameColumn(
                name: "CategoryId",
                table: "CategoryCategoryAttribute",
                newName: "CategoriesId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CategoryCategoryAttribute",
                table: "CategoryCategoryAttribute",
                columns: new[] { "CategoriesId", "CategoryAttributesId" });

            migrationBuilder.CreateIndex(
                name: "IX_CategoryCategoryAttribute_CategoryAttributesId",
                table: "CategoryCategoryAttribute",
                column: "CategoryAttributesId");

            migrationBuilder.AddForeignKey(
                name: "FK_CategoryCategoryAttribute_Categories_CategoriesId",
                table: "CategoryCategoryAttribute",
                column: "CategoriesId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CategoryCategoryAttribute_Categories_CategoriesId",
                table: "CategoryCategoryAttribute");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CategoryCategoryAttribute",
                table: "CategoryCategoryAttribute");

            migrationBuilder.DropIndex(
                name: "IX_CategoryCategoryAttribute_CategoryAttributesId",
                table: "CategoryCategoryAttribute");

            migrationBuilder.RenameColumn(
                name: "CategoriesId",
                table: "CategoryCategoryAttribute",
                newName: "CategoryId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CategoryCategoryAttribute",
                table: "CategoryCategoryAttribute",
                columns: new[] { "CategoryAttributesId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_CategoryCategoryAttribute_CategoryId",
                table: "CategoryCategoryAttribute",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_CategoryCategoryAttribute_Categories_CategoryId",
                table: "CategoryCategoryAttribute",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    public partial class BranchOfficeRefactor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_BranchOffices_BranchOfficeId",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "BranchOfficeId",
                table: "Users",
                newName: "DefaultBranchOfficeId");

            migrationBuilder.RenameIndex(
                name: "IX_Users_BranchOfficeId",
                table: "Users",
                newName: "IX_Users_DefaultBranchOfficeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_BranchOffices_DefaultBranchOfficeId",
                table: "Users",
                column: "DefaultBranchOfficeId",
                principalTable: "BranchOffices",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_BranchOffices_DefaultBranchOfficeId",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "DefaultBranchOfficeId",
                table: "Users",
                newName: "BranchOfficeId");

            migrationBuilder.RenameIndex(
                name: "IX_Users_DefaultBranchOfficeId",
                table: "Users",
                newName: "IX_Users_BranchOfficeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_BranchOffices_BranchOfficeId",
                table: "Users",
                column: "BranchOfficeId",
                principalTable: "BranchOffices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

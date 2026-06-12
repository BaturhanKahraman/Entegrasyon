using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchOfficeStockBranchCurrentStockIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_BranchOfficeStocks_BranchOfficeId_CurrentStock",
                table: "BranchOfficeStocks",
                columns: new[] { "BranchOfficeId", "CurrentStock" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BranchOfficeStocks_BranchOfficeId_CurrentStock",
                table: "BranchOfficeStocks");
        }
    }
}

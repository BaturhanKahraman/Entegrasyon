using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddCargoCompanyApiFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApiKey",
                table: "CargoCompanies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerCode",
                table: "CargoCompanies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "CargoCompanies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsIntegrated",
                table: "CargoCompanies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SecretKey",
                table: "CargoCompanies",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "CargoCompanies",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApiKey", "CustomerCode", "IsDefault", "IsIntegrated", "SecretKey" },
                values: new object[] { null, null, false, false, null });

            migrationBuilder.UpdateData(
                table: "CargoCompanies",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ApiKey", "CustomerCode", "IsDefault", "IsIntegrated", "SecretKey" },
                values: new object[] { null, null, false, false, null });

            migrationBuilder.UpdateData(
                table: "CargoCompanies",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ApiKey", "CustomerCode", "IsDefault", "IsIntegrated", "SecretKey" },
                values: new object[] { null, null, false, false, null });

            migrationBuilder.UpdateData(
                table: "CargoCompanies",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "ApiKey", "CustomerCode", "IsDefault", "IsIntegrated", "SecretKey" },
                values: new object[] { null, null, false, false, null });

            migrationBuilder.UpdateData(
                table: "CargoCompanies",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "ApiKey", "CustomerCode", "IsDefault", "IsIntegrated", "SecretKey" },
                values: new object[] { null, null, false, false, null });

            migrationBuilder.UpdateData(
                table: "CargoCompanies",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "ApiKey", "CustomerCode", "IsDefault", "IsIntegrated", "SecretKey" },
                values: new object[] { null, null, false, false, null });

            migrationBuilder.UpdateData(
                table: "CargoCompanies",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "ApiKey", "CustomerCode", "IsDefault", "IsIntegrated", "SecretKey" },
                values: new object[] { null, null, false, false, null });

            migrationBuilder.UpdateData(
                table: "CargoCompanies",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "ApiKey", "CustomerCode", "IsDefault", "IsIntegrated", "SecretKey" },
                values: new object[] { null, null, false, false, null });

            migrationBuilder.UpdateData(
                table: "CargoCompanies",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "ApiKey", "CustomerCode", "IsDefault", "IsIntegrated", "SecretKey" },
                values: new object[] { null, null, false, false, null });

            migrationBuilder.UpdateData(
                table: "CargoCompanies",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "ApiKey", "CustomerCode", "IsDefault", "IsIntegrated", "SecretKey" },
                values: new object[] { null, null, false, false, null });

            migrationBuilder.UpdateData(
                table: "CargoCompanies",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "ApiKey", "CustomerCode", "IsDefault", "IsIntegrated", "SecretKey" },
                values: new object[] { null, null, false, false, null });

            migrationBuilder.UpdateData(
                table: "CargoCompanies",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "ApiKey", "CustomerCode", "IsDefault", "IsIntegrated", "SecretKey" },
                values: new object[] { null, null, false, false, null });

            migrationBuilder.UpdateData(
                table: "CargoCompanies",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "ApiKey", "CustomerCode", "IsDefault", "IsIntegrated", "SecretKey" },
                values: new object[] { null, null, false, false, null });

            migrationBuilder.UpdateData(
                table: "CargoCompanies",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "ApiKey", "CustomerCode", "IsDefault", "IsIntegrated", "SecretKey" },
                values: new object[] { null, null, false, false, null });

            migrationBuilder.UpdateData(
                table: "CargoCompanies",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "ApiKey", "CustomerCode", "IsDefault", "IsIntegrated", "SecretKey" },
                values: new object[] { null, null, false, false, null });

            migrationBuilder.UpdateData(
                table: "CargoCompanies",
                keyColumn: "Id",
                keyValue: 16,
                columns: new[] { "ApiKey", "CustomerCode", "IsDefault", "IsIntegrated", "SecretKey" },
                values: new object[] { null, null, false, false, null });

            migrationBuilder.UpdateData(
                table: "CargoCompanies",
                keyColumn: "Id",
                keyValue: 17,
                columns: new[] { "ApiKey", "CustomerCode", "IsDefault", "IsIntegrated", "SecretKey" },
                values: new object[] { null, null, false, false, null });

            migrationBuilder.UpdateData(
                table: "CargoCompanies",
                keyColumn: "Id",
                keyValue: 18,
                columns: new[] { "ApiKey", "CustomerCode", "IsDefault", "IsIntegrated", "SecretKey" },
                values: new object[] { null, null, false, false, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApiKey",
                table: "CargoCompanies");

            migrationBuilder.DropColumn(
                name: "CustomerCode",
                table: "CargoCompanies");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "CargoCompanies");

            migrationBuilder.DropColumn(
                name: "IsIntegrated",
                table: "CargoCompanies");

            migrationBuilder.DropColumn(
                name: "SecretKey",
                table: "CargoCompanies");
        }
    }
}

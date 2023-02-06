using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    public partial class RetailCustomerSearchVectorAdded : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SearchVector",
                table: "Customers",
                newName: "RetailSearchVector");

            migrationBuilder.RenameColumn(
                name: "RetailCustomer_SearchVector",
                table: "Customers",
                newName: "CorporateSearchVector");

            migrationBuilder.RenameIndex(
                name: "IX_Customers_SearchVector",
                table: "Customers",
                newName: "IX_Customers_RetailSearchVector");

            migrationBuilder.RenameIndex(
                name: "IX_Customers_RetailCustomer_SearchVector",
                table: "Customers",
                newName: "IX_Customers_CorporateSearchVector");

            migrationBuilder.AlterColumn<NpgsqlTsVector>(
                name: "RetailSearchVector",
                table: "Customers",
                type: "tsvector",
                nullable: true,
                oldClrType: typeof(NpgsqlTsVector),
                oldType: "tsvector",
                oldNullable: true)
                .Annotation("Npgsql:TsVectorConfig", "turkish")
                .Annotation("Npgsql:TsVectorProperties", new[] { "PhoneNumber", "Surname", "Name", "NationalIdentity" })
                .OldAnnotation("Npgsql:TsVectorConfig", "turkish")
                .OldAnnotation("Npgsql:TsVectorProperties", new[] { "PhoneNumber", "CorporateName", "TaxNumber", "Name", "Surname" });

            migrationBuilder.AlterColumn<NpgsqlTsVector>(
                name: "CorporateSearchVector",
                table: "Customers",
                type: "tsvector",
                nullable: true,
                oldClrType: typeof(NpgsqlTsVector),
                oldType: "tsvector",
                oldNullable: true)
                .Annotation("Npgsql:TsVectorConfig", "turkish")
                .Annotation("Npgsql:TsVectorProperties", new[] { "PhoneNumber", "CorporateName", "TaxNumber", "Name", "Surname" })
                .OldAnnotation("Npgsql:TsVectorConfig", "turkish")
                .OldAnnotation("Npgsql:TsVectorProperties", new[] { "PhoneNumber", "Surname", "Name", "NationalIdentity" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RetailSearchVector",
                table: "Customers",
                newName: "SearchVector");

            migrationBuilder.RenameColumn(
                name: "CorporateSearchVector",
                table: "Customers",
                newName: "RetailCustomer_SearchVector");

            migrationBuilder.RenameIndex(
                name: "IX_Customers_RetailSearchVector",
                table: "Customers",
                newName: "IX_Customers_SearchVector");

            migrationBuilder.RenameIndex(
                name: "IX_Customers_CorporateSearchVector",
                table: "Customers",
                newName: "IX_Customers_RetailCustomer_SearchVector");

            migrationBuilder.AlterColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Customers",
                type: "tsvector",
                nullable: true,
                oldClrType: typeof(NpgsqlTsVector),
                oldType: "tsvector",
                oldNullable: true)
                .Annotation("Npgsql:TsVectorConfig", "turkish")
                .Annotation("Npgsql:TsVectorProperties", new[] { "PhoneNumber", "CorporateName", "TaxNumber", "Name", "Surname" })
                .OldAnnotation("Npgsql:TsVectorConfig", "turkish")
                .OldAnnotation("Npgsql:TsVectorProperties", new[] { "PhoneNumber", "Surname", "Name", "NationalIdentity" });

            migrationBuilder.AlterColumn<NpgsqlTsVector>(
                name: "RetailCustomer_SearchVector",
                table: "Customers",
                type: "tsvector",
                nullable: true,
                oldClrType: typeof(NpgsqlTsVector),
                oldType: "tsvector",
                oldNullable: true)
                .Annotation("Npgsql:TsVectorConfig", "turkish")
                .Annotation("Npgsql:TsVectorProperties", new[] { "PhoneNumber", "Surname", "Name", "NationalIdentity" })
                .OldAnnotation("Npgsql:TsVectorConfig", "turkish")
                .OldAnnotation("Npgsql:TsVectorProperties", new[] { "PhoneNumber", "CorporateName", "TaxNumber", "Name", "Surname" });
        }
    }
}

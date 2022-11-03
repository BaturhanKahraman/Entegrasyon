using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    public partial class CustomerRefactor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "TaxPercentage",
                table: "SaleItems",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

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
                .OldAnnotation("Npgsql:TsVectorProperties", new[] { "PhoneNumber", "CorporateName", "TaxNumber" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TaxPercentage",
                table: "SaleItems");

            migrationBuilder.AlterColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "Customers",
                type: "tsvector",
                nullable: true,
                oldClrType: typeof(NpgsqlTsVector),
                oldType: "tsvector",
                oldNullable: true)
                .Annotation("Npgsql:TsVectorConfig", "turkish")
                .Annotation("Npgsql:TsVectorProperties", new[] { "PhoneNumber", "CorporateName", "TaxNumber" })
                .OldAnnotation("Npgsql:TsVectorConfig", "turkish")
                .OldAnnotation("Npgsql:TsVectorProperties", new[] { "PhoneNumber", "CorporateName", "TaxNumber", "Name", "Surname" });
        }
    }
}

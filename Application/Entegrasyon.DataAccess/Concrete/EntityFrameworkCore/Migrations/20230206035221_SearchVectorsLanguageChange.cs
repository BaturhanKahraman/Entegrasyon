using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    public partial class SearchVectorsLanguageChange : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<NpgsqlTsVector>(
                name: "RetailSearchVector",
                table: "Customers",
                type: "tsvector",
                nullable: true,
                oldClrType: typeof(NpgsqlTsVector),
                oldType: "tsvector",
                oldNullable: true)
                .Annotation("Npgsql:TsVectorConfig", "english")
                .Annotation("Npgsql:TsVectorProperties", new[] { "PhoneNumber", "Surname", "Name", "NationalIdentity" })
                .OldAnnotation("Npgsql:TsVectorConfig", "turkish")
                .OldAnnotation("Npgsql:TsVectorProperties", new[] { "PhoneNumber", "Surname", "Name", "NationalIdentity" });

            migrationBuilder.AlterColumn<NpgsqlTsVector>(
                name: "CorporateSearchVector",
                table: "Customers",
                type: "tsvector",
                nullable: true,
                oldClrType: typeof(NpgsqlTsVector),
                oldType: "tsvector",
                oldNullable: true)
                .Annotation("Npgsql:TsVectorConfig", "english")
                .Annotation("Npgsql:TsVectorProperties", new[] { "PhoneNumber", "CorporateName", "TaxNumber", "Name", "Surname" })
                .OldAnnotation("Npgsql:TsVectorConfig", "turkish")
                .OldAnnotation("Npgsql:TsVectorProperties", new[] { "PhoneNumber", "CorporateName", "TaxNumber", "Name", "Surname" });

            migrationBuilder.AlterColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "CargoCompanies",
                type: "tsvector",
                nullable: true,
                oldClrType: typeof(NpgsqlTsVector),
                oldType: "tsvector",
                oldNullable: true)
                .Annotation("Npgsql:TsVectorConfig", "english")
                .Annotation("Npgsql:TsVectorProperties", new[] { "Code", "Name", "TaxNumber" })
                .OldAnnotation("Npgsql:TsVectorConfig", "turkish")
                .OldAnnotation("Npgsql:TsVectorProperties", new[] { "Code", "Name", "TaxNumber" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                .OldAnnotation("Npgsql:TsVectorConfig", "english")
                .OldAnnotation("Npgsql:TsVectorProperties", new[] { "PhoneNumber", "Surname", "Name", "NationalIdentity" });

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
                .OldAnnotation("Npgsql:TsVectorConfig", "english")
                .OldAnnotation("Npgsql:TsVectorProperties", new[] { "PhoneNumber", "CorporateName", "TaxNumber", "Name", "Surname" });

            migrationBuilder.AlterColumn<NpgsqlTsVector>(
                name: "SearchVector",
                table: "CargoCompanies",
                type: "tsvector",
                nullable: true,
                oldClrType: typeof(NpgsqlTsVector),
                oldType: "tsvector",
                oldNullable: true)
                .Annotation("Npgsql:TsVectorConfig", "turkish")
                .Annotation("Npgsql:TsVectorProperties", new[] { "Code", "Name", "TaxNumber" })
                .OldAnnotation("Npgsql:TsVectorConfig", "english")
                .OldAnnotation("Npgsql:TsVectorProperties", new[] { "Code", "Name", "TaxNumber" });
        }
    }
}

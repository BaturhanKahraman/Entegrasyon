using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingImageColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Storage
            migrationBuilder.AddColumn<string>(
                name: "StorageKey",
                table: "Images",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            // Metadata
            migrationBuilder.AddColumn<int>(
                name: "OriginalWidth",
                table: "Images",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OriginalHeight",
                table: "Images",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "FileSizeBytes",
                table: "Images",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "Images",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            // Display
            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "Images",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Processing flags
            migrationBuilder.AddColumn<bool>(
                name: "ThumbnailGenerated",
                table: "Images",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MediumGenerated",
                table: "Images",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LargeGenerated",
                table: "Images",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessedAt",
                table: "Images",
                type: "timestamp with time zone",
                nullable: true);

            // Description maxLength 100 → 200
            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Images",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "StorageKey", table: "Images");
            migrationBuilder.DropColumn(name: "OriginalWidth", table: "Images");
            migrationBuilder.DropColumn(name: "OriginalHeight", table: "Images");
            migrationBuilder.DropColumn(name: "FileSizeBytes", table: "Images");
            migrationBuilder.DropColumn(name: "ContentType", table: "Images");
            migrationBuilder.DropColumn(name: "DisplayOrder", table: "Images");
            migrationBuilder.DropColumn(name: "ThumbnailGenerated", table: "Images");
            migrationBuilder.DropColumn(name: "MediumGenerated", table: "Images");
            migrationBuilder.DropColumn(name: "LargeGenerated", table: "Images");
            migrationBuilder.DropColumn(name: "ProcessedAt", table: "Images");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Images",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);
        }
    }
}

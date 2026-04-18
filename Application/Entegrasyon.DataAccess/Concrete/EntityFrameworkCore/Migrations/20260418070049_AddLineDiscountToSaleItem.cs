using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddLineDiscountToSaleItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "SaleItems",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DiscountReasonId",
                table: "SaleItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiscountReasonNote",
                table: "SaleItems",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DiscountReasons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountReasons", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SaleItems_DiscountReasonId",
                table: "SaleItems",
                column: "DiscountReasonId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountReasons_Name",
                table: "DiscountReasons",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleItems_DiscountReasons_DiscountReasonId",
                table: "SaleItems",
                column: "DiscountReasonId",
                principalTable: "DiscountReasons",
                principalColumn: "Id");

            // Varsayılan standart indirim nedenleri
            migrationBuilder.Sql(@"
                INSERT INTO ""DiscountReasons"" (""Name"", ""IsActive"", ""IsDeleted"", ""DeletedAt"", ""CreatedAt"", ""UpdatedAt"")
                VALUES
                    ('Müşteri isteği',    true, false, 'epoch'::timestamptz, now(), now()),
                    ('Ürün hasarı',       true, false, 'epoch'::timestamptz, now(), now()),
                    ('Promosyon',         true, false, 'epoch'::timestamptz, now(), now()),
                    ('Çalışan indirimi',  true, false, 'epoch'::timestamptz, now(), now()),
                    ('Toplu alım',        true, false, 'epoch'::timestamptz, now(), now()),
                    ('Vade indirimi',     true, false, 'epoch'::timestamptz, now(), now());
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SaleItems_DiscountReasons_DiscountReasonId",
                table: "SaleItems");

            migrationBuilder.DropTable(
                name: "DiscountReasons");

            migrationBuilder.DropIndex(
                name: "IX_SaleItems_DiscountReasonId",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "DiscountReasonId",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "DiscountReasonNote",
                table: "SaleItems");
        }
    }
}

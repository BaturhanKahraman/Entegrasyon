using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class _CheckPending : Migration
    {
        /// <inheritdoc />
        /// <summary>
        /// Snapshot senkronizasyon migration'ı.
        /// Tablolar, kolonlar ve çoğu index zaten DB'de mevcut (SQL ile uygulanmış).
        /// Sadece gerçekten eksik olan index değişiklikleri uygulanır.
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // IX_Categories_ImportId -> IX_Categories_ExternalCategoryId değişimi
            migrationBuilder.DropIndex(
                name: "IX_Categories_ImportId",
                table: "Categories");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ExternalCategoryId",
                table: "Categories",
                column: "ExternalCategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Categories_ExternalCategoryId",
                table: "Categories");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ImportId",
                table: "Categories",
                column: "ImportId");
        }
    }
}

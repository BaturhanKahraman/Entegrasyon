using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class SecurityExpansionV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastPasswordChangedAt",
                table: "StorefrontCustomerAuths",
                type: "timestamp with time zone",
                nullable: true);

            // Entity initializer'i = true; mevcut Kullanıcılar da giris bildirimlerini acik devralsin.
            migrationBuilder.AddColumn<bool>(
                name: "LoginAlertsEnabled",
                table: "StorefrontCustomerAuths",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastPasswordChangedAt",
                table: "StorefrontCustomerAuths");

            migrationBuilder.DropColumn(
                name: "LoginAlertsEnabled",
                table: "StorefrontCustomerAuths");
        }
    }
}

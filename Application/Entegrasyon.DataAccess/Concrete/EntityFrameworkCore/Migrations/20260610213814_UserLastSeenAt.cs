using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class UserLastSeenAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastSeenAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("dfda5d4a-f807-408c-9b4d-908830ad5724"),
                column: "LastSeenAt",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastSeenAt",
                table: "Users");
        }
    }
}

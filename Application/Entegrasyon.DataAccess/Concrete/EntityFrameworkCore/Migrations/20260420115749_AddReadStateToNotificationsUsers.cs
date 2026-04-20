using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddReadStateToNotificationsUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRead",
                table: "NotificationsUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReadAt",
                table: "NotificationsUsers",
                type: "timestamp with time zone",
                nullable: true);

            // Backfill: sync per-user read state from global Notification.IsRead/ReadAt
            migrationBuilder.Sql(@"
                UPDATE ""NotificationsUsers"" nu
                SET ""IsRead"" = n.""IsRead"", ""ReadAt"" = CASE WHEN n.""IsRead"" THEN n.""ReadAt"" ELSE NULL END
                FROM ""Notifications"" n
                WHERE nu.""NotificationId"" = n.""Id"";
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRead",
                table: "NotificationsUsers");

            migrationBuilder.DropColumn(
                name: "ReadAt",
                table: "NotificationsUsers");
        }
    }
}

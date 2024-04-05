using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class NotificationSettingsAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Claims_ApplicationClaimId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Users_ApplicationUserId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_ApplicationClaimId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_ApplicationUserId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ApplicationClaimId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Notifications");

            migrationBuilder.RenameColumn(
                name: "ReadDate",
                table: "Notifications",
                newName: "ReadAt");

            migrationBuilder.CreateTable(
                name: "NotificationsClaims",
                columns: table => new
                {
                    NotificationId = table.Column<long>(type: "bigint", nullable: false),
                    ClaimId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationsClaims", x => new { x.ClaimId, x.NotificationId });
                    table.ForeignKey(
                        name: "FK_NotificationsClaims_Claims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "Claims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotificationsClaims_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationsUsers",
                columns: table => new
                {
                    ApplicationUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationsUsers", x => new { x.ApplicationUserId, x.NotificationId });
                    table.ForeignKey(
                        name: "FK_NotificationsUsers_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotificationsUsers_Users_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("dfda5d4a-f807-408c-9b4d-908830ad5724"),
                column: "NormalizedUserName",
                value: "ADMIN");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationsClaims_NotificationId",
                table: "NotificationsClaims",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationsUsers_NotificationId",
                table: "NotificationsUsers",
                column: "NotificationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationsClaims");

            migrationBuilder.DropTable(
                name: "NotificationsUsers");

            migrationBuilder.RenameColumn(
                name: "ReadAt",
                table: "Notifications",
                newName: "ReadDate");

            migrationBuilder.AddColumn<int>(
                name: "ApplicationClaimId",
                table: "Notifications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApplicationUserId",
                table: "Notifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("dfda5d4a-f807-408c-9b4d-908830ad5724"),
                column: "NormalizedUserName",
                value: "Admin");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ApplicationClaimId",
                table: "Notifications",
                column: "ApplicationClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ApplicationUserId",
                table: "Notifications",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Claims_ApplicationClaimId",
                table: "Notifications",
                column: "ApplicationClaimId",
                principalTable: "Claims",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Users_ApplicationUserId",
                table: "Notifications",
                column: "ApplicationUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}

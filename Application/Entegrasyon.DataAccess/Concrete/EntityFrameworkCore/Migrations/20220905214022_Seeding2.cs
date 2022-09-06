using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    public partial class Seeding2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("dfda5d4a-f807-408c-9b4d-908830ad5724"));

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "DefaultBranchOfficeId", "Discriminator", "Email", "IsActive", "IsDeleted", "IsTwoFactorAuthActive", "MobileJwtToken", "MobileJwtTokenExpiresAt", "Name", "NeedsTakeNewPassword", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PasswordSalt", "RootClaimId", "Surname", "TemporaryPassword", "UserName", "WebJwtToken", "WebJwtTokenExpiresAt" },
                values: new object[] { new Guid("dfda5d4a-f807-408c-9b4d-908830ad5724"), new DateTimeOffset(new DateTime(2022, 9, 6, 0, 40, 21, 544, DateTimeKind.Unspecified).AddTicks(5429), new TimeSpan(0, 3, 0, 0, 0)), null, "ApplicationUser", "admin@admin.com", true, false, false, null, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Admin", true, null, "ADMIN", null, null, null, "Admin", "Admin", "Admin", null, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("dfda5d4a-f807-408c-9b4d-908830ad5724"));

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Discriminator", "Email", "IsActive", "IsDeleted", "IsTwoFactorAuthActive", "MobileJwtToken", "MobileJwtTokenExpiresAt", "Name", "NeedsTakeNewPassword", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PasswordSalt", "RootClaimId", "Surname", "TemporaryPassword", "UserName", "WebJwtToken", "WebJwtTokenExpiresAt" },
                values: new object[] { new Guid("dfda5d4a-f807-408c-9b4d-908830ad5724"), new DateTimeOffset(new DateTime(2022, 9, 5, 23, 59, 47, 651, DateTimeKind.Unspecified).AddTicks(2827), new TimeSpan(0, 3, 0, 0, 0)), "RootUser", "admin@admin.com", true, false, false, null, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Admin", true, null, "ADMIN", null, null, null, "Admin", "Admin", "Admin", null, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });
        }
    }
}

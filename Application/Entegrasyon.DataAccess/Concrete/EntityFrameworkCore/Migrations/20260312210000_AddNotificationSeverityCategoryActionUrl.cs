using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <summary>
    /// Notification tablosuna Severity, Category ve ActionUrl sütunlarını ekler.
    /// Header max-length 50→200, Content max-length 400→1000 olarak güncellenir.
    /// </summary>
    public partial class AddNotificationSeverityCategoryActionUrl : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Yeni sütunlar
            migrationBuilder.Sql("""
                ALTER TABLE "Notifications" ADD COLUMN IF NOT EXISTS "Severity" integer NOT NULL DEFAULT 0;
                ALTER TABLE "Notifications" ADD COLUMN IF NOT EXISTS "Category" integer NOT NULL DEFAULT 0;
                ALTER TABLE "Notifications" ADD COLUMN IF NOT EXISTS "ActionUrl" character varying(500) NULL;
                """);

            // Max-length güncellemeleri
            migrationBuilder.Sql("""
                ALTER TABLE "Notifications" ALTER COLUMN "Header" TYPE character varying(200);
                ALTER TABLE "Notifications" ALTER COLUMN "Content" TYPE character varying(1000);
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Notifications" DROP COLUMN IF EXISTS "Severity";
                ALTER TABLE "Notifications" DROP COLUMN IF EXISTS "Category";
                ALTER TABLE "Notifications" DROP COLUMN IF EXISTS "ActionUrl";
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "Notifications" ALTER COLUMN "Header" TYPE character varying(50);
                ALTER TABLE "Notifications" ALTER COLUMN "Content" TYPE character varying(400);
                """);
        }
    }
}

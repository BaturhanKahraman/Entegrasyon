using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class SalesModuleSeedPaymentMethods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "PaymentMethodDefinitions" ("Name", "SystemCode", "Icon", "IsActive", "SortOrder", "CommissionRate", "RequiresAuthCode", "RequiresCashInput", "TenantId", "IsDeleted", "DeletedAt", "CreatedAt", "UpdatedAt")
                SELECT * FROM (VALUES
                    ('Nakit',       'Cash',         'ti ti-cash',              true, 1, NULL::numeric, false, true,  1, false, '-infinity'::timestamptz, NOW(), NOW()),
                    ('Kredi Kartı', 'CreditCard',   'ti ti-credit-card',       true, 2, NULL::numeric, true,  false, 1, false, '-infinity'::timestamptz, NOW(), NOW()),
                    ('Banka Kartı', 'DebitCard',    'ti ti-credit-card',       true, 3, NULL::numeric, true,  false, 1, false, '-infinity'::timestamptz, NOW(), NOW()),
                    ('Yemek Kartı', 'MealCard',     'ti ti-tools-kitchen-2',   true, 4, NULL::numeric, true,  false, 1, false, '-infinity'::timestamptz, NOW(), NOW()),
                    ('Havale/EFT',  'BankTransfer', 'ti ti-building-bank',     true, 5, NULL::numeric, true,  false, 1, false, '-infinity'::timestamptz, NOW(), NOW())
                ) AS v("Name", "SystemCode", "Icon", "IsActive", "SortOrder", "CommissionRate", "RequiresAuthCode", "RequiresCashInput", "TenantId", "IsDeleted", "DeletedAt", "CreatedAt", "UpdatedAt")
                WHERE NOT EXISTS (
                    SELECT 1 FROM "PaymentMethodDefinitions"
                    WHERE "TenantId" = v."TenantId" AND "SystemCode" = v."SystemCode"
                );
            """);

            migrationBuilder.Sql("""
                UPDATE "Sales"
                SET "SaleStatus" = 1,
                    "SaleSource" = 1,
                    "SaleDate" = "CreatedAt",
                    "SaleNumber" = 'LEGACY-' || "Id"::text
                WHERE "SaleNumber" = '' OR "SaleNumber" IS NULL;
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "PaymentMethodDefinitions"
                WHERE "TenantId" = 1
                  AND "SystemCode" IN ('Cash', 'CreditCard', 'DebitCard', 'MealCard', 'BankTransfer');
            """);
        }
    }
}

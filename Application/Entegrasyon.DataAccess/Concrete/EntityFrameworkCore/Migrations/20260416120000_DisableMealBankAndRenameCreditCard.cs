using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class DisableMealBankAndRenameCreditCard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "PaymentMethodDefinitions"
                   SET "IsActive" = false, "UpdatedAt" = NOW()
                 WHERE "SystemCode" IN ('MealCard','DebitCard');
            """);

            migrationBuilder.Sql("""
                UPDATE "PaymentMethodDefinitions"
                   SET "Name" = 'Kredi/Banka Kartı', "UpdatedAt" = NOW()
                 WHERE "SystemCode" = 'CreditCard';
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "PaymentMethodDefinitions"
                   SET "Name" = 'Kredi Kartı', "UpdatedAt" = NOW()
                 WHERE "SystemCode" = 'CreditCard';
            """);

            migrationBuilder.Sql("""
                UPDATE "PaymentMethodDefinitions"
                   SET "IsActive" = true, "UpdatedAt" = NOW()
                 WHERE "SystemCode" IN ('MealCard','DebitCard');
            """);
        }
    }
}

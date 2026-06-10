using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <summary>
    /// AddSecurityStampToApplicationUser SecurityStamp kolonunu NULLABLE ekledi → mevcut tüm Users
    /// satırları NULL kaldı. SecurityStampCookieEvents.ValidatePrincipal fail-closed olduğundan,
    /// NULL stamp'li kullanıcı login olabilse de (cookie'ye yeni stamp yazılır) DB NULL kaldığı için
    /// SONRAKİ her authenticated istekte reddedilir → sonsuz logout loop, app kullanılamaz.
    ///
    /// Bu migration NULL stamp'leri 32 karakterlik (Guid.ToString("N") formatında) bir değerle doldurur.
    /// Login backfill (AuthService) asıl kök-neden fix'idir; bu migration ise mevcut satırları deploy
    /// anında (--migrate) tüm tenant DB'lerinde kalıcı olarak onarır — login beklemeden.
    ///
    /// Data-only (model değişmez) → snapshot etkilenmez, has-pending-model-changes temiz kalır.
    /// IDEMPOTENT: yalnızca WHERE "SecurityStamp" IS NULL satırlarına yazar; tekrar çalışırsa no-op.
    /// </summary>
    public partial class BackfillSecurityStamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // gen_random_uuid() → pgcrypto/PG13+ yerleşik; '-' temizlenince 32 hex karakter (varchar(32)).
            // Her satıra benzersiz değer üretmek için set-bazlı (kolon ifadesi) UPDATE.
            migrationBuilder.Sql(
                """
                UPDATE "Users"
                SET "SecurityStamp" = replace(gen_random_uuid()::text, '-', '')
                WHERE "SecurityStamp" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data backfill geri alınamaz (orijinal NULL durumu güvenlik açığıydı). No-op.
        }
    }
}

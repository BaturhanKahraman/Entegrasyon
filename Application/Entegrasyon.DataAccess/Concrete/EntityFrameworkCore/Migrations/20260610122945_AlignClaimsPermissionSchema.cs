using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AlignClaimsPermissionSchema : Migration
    {
        // REPAIR MIGRATION (idempotent raw SQL).
        //
        // SORUN: RolesClaims.Permission / UsersClaims.Permission kolonları ve yeni PK
        // (string-tabanlı izin modeli) ModelSnapshot + tüm Designer snapshot'larında VAR,
        // ama hiçbir migration .cs'i bu DDL'i UYGULAMIYORDU. Snapshot, migration geçmişinin
        // önüne geçmişti. Sonuç: taze migrate edilen DB'de kolon YOK → AdminPermissionSeeder
        // (context.Roles.Include(RoleClaims)...Permission) Npgsql 42703 ile boot'ta patlıyordu.
        //
        // Bu migration migration geçmişini snapshot ile hizalar.
        //
        // İDEMPOTENT: Bazı yerel DB'ler erken EnsureCreated/elle ekleme ile kolonu ZATEN
        // içerebilir. Bu yüzden ADD COLUMN IF NOT EXISTS + DROP CONSTRAINT/INDEX IF EXISTS +
        // koşullu DELETE kullanılır → hem taze (eski şema) hem mevcut (yeni şema) DB güvenli.
        //
        // Legacy veri: InitialCreate, RolesClaims'e 18 satır seed eder (hepsi RoleId=1,
        // ApplicationClaimId=1..18, Permission yok). Yeni PK (RoleId, Permission) bunları
        // (1, '') olarak çakıştırırdı. Bu yüzden Permission'ı ÖNCE nullable ekleyip
        // Permission IS NULL legacy satırları sileriz (string-Permission satırları korunur);
        // AdminPermissionSeeder boot'ta string izinleri yeniden doldurur.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── RolesClaims ───────────────────────────────────────────────
            migrationBuilder.Sql(@"
                -- Legacy Claims FK'sini kaldir (yeni modelde ApplicationClaimId FK'siz nullable kolon)
                ALTER TABLE ""RolesClaims"" DROP CONSTRAINT IF EXISTS ""FK_RolesClaims_Claims_ApplicationClaimId"";

                -- Permission kolonu (once nullable — legacy satirlar NULL alsin)
                ALTER TABLE ""RolesClaims"" ADD COLUMN IF NOT EXISTS ""Permission"" character varying(256);

                -- Legacy (int-claim tabanli, Permission'i olmayan) satirlari temizle.
                -- Yeni-sema DB'sinde bu satir yoktur (tum satirlarin Permission'i dolu); seeder yeniden doldurur.
                DELETE FROM ""RolesClaims"" WHERE ""Permission"" IS NULL;

                -- Artik NULL yok → Permission NOT NULL (PK uyesi). Zaten NOT NULL ise no-op.
                ALTER TABLE ""RolesClaims"" ALTER COLUMN ""Permission"" SET NOT NULL;

                -- RoleId artik PK'nin lider kolonu → ayri index gereksiz
                DROP INDEX IF EXISTS ""IX_RolesClaims_RoleId"";

                -- ESKI PK'yi ONCE dusur: ApplicationClaimId eski PK uyesi oldugu icin
                -- PK durmadan DROP NOT NULL Postgres'te 42P16 verir.
                ALTER TABLE ""RolesClaims"" DROP CONSTRAINT IF EXISTS ""PK_RolesClaims"";

                -- ApplicationClaimId artik opsiyonel (legacy/deprecated) — PK'den ciktiktan sonra
                ALTER TABLE ""RolesClaims"" ALTER COLUMN ""ApplicationClaimId"" DROP NOT NULL;

                -- Yeni PK: (RoleId, Permission)
                ALTER TABLE ""RolesClaims"" ADD CONSTRAINT ""PK_RolesClaims"" PRIMARY KEY (""RoleId"", ""Permission"");
            ");

            // ── UsersClaims ───────────────────────────────────────────────
            migrationBuilder.Sql(@"
                ALTER TABLE ""UsersClaims"" DROP CONSTRAINT IF EXISTS ""FK_UsersClaims_Claims_ApplicationClaimId"";

                ALTER TABLE ""UsersClaims"" ADD COLUMN IF NOT EXISTS ""Permission"" character varying(256);

                DELETE FROM ""UsersClaims"" WHERE ""Permission"" IS NULL;

                ALTER TABLE ""UsersClaims"" ALTER COLUMN ""Permission"" SET NOT NULL;

                DROP INDEX IF EXISTS ""IX_UsersClaims_ApplicationUserId"";

                -- ESKI PK'yi ONCE dusur (ApplicationClaimId eski PK uyesi → 42P16 onlemek icin)
                ALTER TABLE ""UsersClaims"" DROP CONSTRAINT IF EXISTS ""PK_UsersClaims"";

                ALTER TABLE ""UsersClaims"" ALTER COLUMN ""ApplicationClaimId"" DROP NOT NULL;

                ALTER TABLE ""UsersClaims"" ADD CONSTRAINT ""PK_UsersClaims"" PRIMARY KEY (""ApplicationUserId"", ""Permission"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Kasitli NO-OP: Bu migration'dan ONCEKI migration'in (Designer) snapshot'i zaten
            // yeni semayi (Permission + yeni PK) tanimliyor. Yani EF'in "bu migration'dan onceki
            // model durumu" = yeni sema. DB'yi bu duruma geri dondurmek icin Down'in bir sey
            // yapmamasi gerekir; aksi halde sema, snapshot zinciriyle desenkronize olurdu.
            // (Geriye donuk sema yeniden insasi ayrica legacy veride NOT NULL/FK geri-ekleme ile
            // patlardi; bu repair migration'in kapsami disi.)
        }
    }
}

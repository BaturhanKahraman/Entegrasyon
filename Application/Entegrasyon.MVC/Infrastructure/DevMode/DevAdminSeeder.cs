using Entegrasyon.Business.Utilities;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.MVC.Infrastructure.DevMode;

/// <summary>
/// Development ortaminda admin kullanicisina bilinen bir parola (admin / 123456789)
/// atayarak login'i calisir kilar. MVC startup'ta calistirilir (SADECE Development).
///
/// NEDEN: Admin user prod HasData seed'i ile sabit Id ile gelir, ANCAK fresh DB'de
/// NeedsTakeNewPassword=true + BcryptPasswordHash=NULL durumundadir → reset-stub
/// dead-end → login imkansiz. Dev DB sifirdan kurulursa (volume silinir / fresh deploy)
/// login yine kirilir. Bu seeder o durumu her boot'ta otomatik onarir.
///
/// ⚠️ GUVENLIK: Sadece IsDevelopment() kosulunda calistirilmalidir (Program.cs entegrasyonu
/// bunu garanti eder). PROD HasData seed'ine DOKUNMAZ — prod'da admin hala
/// NeedsTakeNewPassword=true onboarding akisiyla gelmeli. Bu kod prod'da CALISTIRILMAZ.
///
/// IDEMPOTENT: Admin zaten kullanilabilir parolayla provision edilmisse (bcrypt set +
/// PasswordHashVersion=1 + NeedsTakeNewPassword=false) hicbir sey yazmaz — boylece
/// gelistiricinin app uzerinden manuel degistirdigi parolayi da EZMEZ.
/// </summary>
public static class DevAdminSeeder
{
    /// <summary>Admin'in normalize edilmis kullanici adi (AuthService bununla arar).</summary>
    private const string AdminNormalizedUserName = "ADMIN";
    private const string AdminUserName = "Admin";
    private const string DevPassword = "123456789";

    public static async Task SeedAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<IntegrationDbContext>>();
            await using var db = contextFactory.CreateDbContext();

            // AsTracking SART: context global no-tracking (TenantDbContextFactory) →
            // tracking'siz okunan user'daki degisiklik SaveChanges'te no-op olur (bkz. 0ad50995).
            var admin = await db.Users
                .AsTracking()
                .FirstOrDefaultAsync(u => u.NormalizedUserName == AdminNormalizedUserName);

            if (admin is null)
            {
                logger.LogWarning(
                    "DevAdminSeeder: '{UserName}' kullanicisi bulunamadı — parola seed atlandi. " +
                    "(Migration HasData admin'i saglamali.)", AdminUserName);
                return;
            }

            // Idempotency: zaten kullanilabilir bir bcrypt parolasi varsa dokunma.
            var alreadyUsable =
                !admin.NeedsTakeNewPassword &&
                admin.PasswordHashVersion == 1 &&
                !string.IsNullOrEmpty(admin.BcryptPasswordHash);

            if (alreadyUsable)
            {
                logger.LogDebug("DevAdminSeeder: admin zaten kullanilabilir parolayla provision edilmis, degisiklik yok.");
                return;
            }

            // Parolayi app'in kendi helper'iyla seed-time'da uret (BCrypt workFactor 12).
            admin.BcryptPasswordHash = HashingHelper.CreateBcryptHash(DevPassword);
            // Fresh dev DB'de SecurityStamp NULL kalmasin → SecurityStampCookieEvents
            // login sonrasi oturumu reddetmesin (auto-logout loop). Login backfill bunu zaten
            // garanti eder; burada seed-time'da da set ederek dev'i tutarli baslat.
            if (string.IsNullOrEmpty(admin.SecurityStamp))
                admin.SecurityStamp = Guid.NewGuid().ToString("N");
            admin.PasswordHashVersion = 1;
            admin.NeedsTakeNewPassword = false;
            admin.PasswordHash = null;       // legacy HMACSHA512 alanlarini temizle
            admin.PasswordSalt = null;
            admin.TemporaryPassword = null;
            admin.UserName = AdminUserName;
            admin.NormalizedUserName = AdminNormalizedUserName;
            admin.IsActive = true;
            admin.FailedLoginCount = 0;       // varsa kilidi sifirla
            admin.LockoutEnd = null;

            await db.SaveChangesAsync();

            logger.LogInformation(
                "DevAdminSeeder: admin parolasi dev icin set edildi (kullanici: {UserName} / 123456789).",
                AdminUserName);
        }
        catch (Exception ex)
        {
            // DB baglantisi yoksa MVC normal calismaya devam etsin — log'la ve gec.
            logger.LogWarning(ex, "DevAdminSeeder: admin parola seed başarısız, gecillenecek. Hata: {Message}", ex.Message);
        }
    }
}

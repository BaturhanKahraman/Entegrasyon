using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.ApplicationBootstrap.Security;

/// <summary>
/// Startup'ta çalışan idempotent Admin permission seeder.
///
/// Amaç: <see cref="AppPermissions.GetAllPermissions"/> listesine yeni claim eklendiğinde
/// Admin role'ün DB'deki claim'lerinin otomatik senkronize olmasını sağlamak.
///
/// Drift pattern'i (kod ile DB'nin kayması), Faz 1'de "kod ekle, DB unut" şeklinde bir bug doğurmuştu:
/// yeni permission tanımlandığı halde hiçbir kullanıcıda bulunmadığı için "Onayla" butonu render olmuyordu.
/// Bu seeder o bug'ın tekrarlamasını engeller.
///
/// Davranış:
/// - SADECE EKLEME yapar — hiçbir zaman silmez (kullanıcının manuel özelleştirmesini bozmamak için)
/// - Admin role yoksa hiçbir şey yapmaz (ilk kurulum senaryosu RoleService üzerinden yönetilir)
/// - Idempotent: her çağrıldığında aynı sonucu verir, fazladan yazım yapmaz
/// </summary>
public class AdminPermissionSeeder(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    ILogger<AdminPermissionSeeder> logger)
{
    private const string AdminRoleName = "Admin";

    public async Task EnsureAdminPermissionsAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        var adminRole = await context.Roles
            .Include(r => r.RoleClaims)
            .FirstOrDefaultAsync(r => r.Name == AdminRoleName, ct);

        if (adminRole is null)
        {
            logger.LogInformation(
                "Admin role bulunamadı — permission seed atlandı. İlk kurulum RoleService üzerinden yapılmalı.");
            return;
        }

        var existingPermissions = adminRole.RoleClaims
            .Where(c => c.Permission is not null)
            .Select(c => c.Permission!)
            .ToList();

        var expectedPermissions = AppPermissions.GetAllPermissions();

        var missing = PermissionSyncCalculator.GetMissingPermissions(existingPermissions, expectedPermissions);

        if (missing.Count == 0)
        {
            logger.LogDebug("Admin role permission'ları güncel ({Count} claim).", existingPermissions.Count);
            return;
        }

        foreach (var permission in missing)
        {
            adminRole.RoleClaims.Add(new RolesClaims { Permission = permission });
        }

        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Admin role'e {Count} yeni permission eklendi: {Permissions}",
            missing.Count,
            string.Join(", ", missing));
    }
}

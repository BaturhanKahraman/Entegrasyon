using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.User;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Notifications;

public class NotificationRecipientResolver(IDbContextFactory<IntegrationDbContext> contextFactory)
    : INotificationRecipientResolver
{
    public async Task<List<Guid>> ResolveByRoleAsync(int roleId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Set<UsersRoles>()
            .Where(ur => ur.RoleId == roleId)
            .Select(ur => ur.ApplicationUserId)
            .ToListAsync();
    }

    public async Task<List<Guid>> ResolveByBranchOfficeAsync(int branchOfficeId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Users
            .Where(u => u.DefaultBranchOfficeId == branchOfficeId && u.IsActive)
            .Select(u => u.Id)
            .ToListAsync();
    }

    public async Task<List<Guid>> ResolveAllActiveUsersAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.Users
            .Where(u => u.IsActive)
            .Select(u => u.Id)
            .ToListAsync();
    }

    public async Task<List<Guid>> ResolveByPermissionAsync(string permission, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct);

        // (a) Kullanıcının bir rolü var, o rolün de bu permission'ı var
        var viaRoles = await context.Users
            .Where(u => u.IsActive && !u.IsDeleted)
            .Where(u => context.Set<UsersRoles>().Any(ur =>
                ur.ApplicationUserId == u.Id &&
                context.Set<RolesClaims>().Any(rc =>
                    rc.RoleId == ur.RoleId && rc.Permission == permission)))
            .Select(u => u.Id)
            .ToListAsync(ct);

        // (b) Kullanıcıya doğrudan claim atanmış
        var viaDirect = await context.Users
            .Where(u => u.IsActive && !u.IsDeleted)
            .Where(u => context.Set<UsersClaims>().Any(uc =>
                uc.ApplicationUserId == u.Id && uc.Permission == permission))
            .Select(u => u.Id)
            .ToListAsync(ct);

        // Union + distinct (Guid için HashSet otomatik distinct garanti eder)
        return viaRoles.Union(viaDirect).ToList();
    }
}

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
}

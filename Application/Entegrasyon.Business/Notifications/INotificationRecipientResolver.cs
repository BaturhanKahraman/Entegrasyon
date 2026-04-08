namespace Entegrasyon.Business.Notifications;

public interface INotificationRecipientResolver
{
    Task<List<Guid>> ResolveByRoleAsync(int roleId);
    Task<List<Guid>> ResolveByBranchOfficeAsync(int branchOfficeId);
    Task<List<Guid>> ResolveAllActiveUsersAsync();

    /// <summary>
    /// Belirtilen permission claim'ine sahip tüm aktif kullanıcıları döner.
    /// Union of: (a) RolesClaims üzerinden o permission'a sahip rol kullanıcıları,
    ///           (b) UsersClaims üzerinden doğrudan permission atanmış kullanıcılar.
    /// Sadece IsActive && !IsDeleted kullanıcılar listelenir.
    /// </summary>
    Task<List<Guid>> ResolveByPermissionAsync(string permission, CancellationToken ct = default);
}

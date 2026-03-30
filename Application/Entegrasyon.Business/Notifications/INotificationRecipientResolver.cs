namespace Entegrasyon.Business.Notifications;

public interface INotificationRecipientResolver
{
    Task<List<Guid>> ResolveByRoleAsync(int roleId);
    Task<List<Guid>> ResolveByBranchOfficeAsync(int branchOfficeId);
    Task<List<Guid>> ResolveAllActiveUsersAsync();
}

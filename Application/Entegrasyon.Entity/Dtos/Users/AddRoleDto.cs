namespace Entegrasyon.Entity.Dtos.Users
{
    public sealed record AddRoleDto(string Name, List<string> PermissionNames);
}

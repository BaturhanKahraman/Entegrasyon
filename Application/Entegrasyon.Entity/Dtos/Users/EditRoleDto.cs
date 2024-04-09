namespace Entegrasyon.Entity.Dtos.Users
{
    public sealed record EditRoleDto(int Id,string Name,List<int> Claims);
}

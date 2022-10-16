using Shared.User;

namespace Entegrasyon.Entity.Dtos.Users
{
    public sealed class AddRoleDto
    {
        public string Name { get; set; }
        public List<int> Claims { get; set; }
    }
}

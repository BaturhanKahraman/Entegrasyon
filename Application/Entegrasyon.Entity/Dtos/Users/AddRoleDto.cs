using Shared.User;

namespace Entegrasyon.Entity.Dtos.Users
{
    public class AddRoleDto
    {
        public string RoleName { get; set; }
        public List<RootClaim> RootClaims { get; set; }
    }
}

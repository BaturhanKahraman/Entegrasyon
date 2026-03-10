using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.Entity.User;

public class UsersRoles
{
    public Guid ApplicationUserId { get; set; }
    public ApplicationUser ApplicationUser { get; set; }
    public int RoleId { get; set; }
    public Role Role { get; set; }
}
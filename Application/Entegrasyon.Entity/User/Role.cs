using System.Text.Json.Serialization;

namespace Entegrasyon.Entity.User;

public class Role : BaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string NormalizedName { get; set; } = null!;
    public ICollection<RolesClaims> RoleClaims { get; set; } = [];
    public ICollection<UsersRoles> UsersRoles { get; set; } = [];
    public ICollection<ApplicationUser> Users { get; set; } = [];
}

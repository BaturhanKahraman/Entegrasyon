using System.Text.Json.Serialization;

namespace Entegrasyon.Entity.User;

public class Role : BaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? NormalizedName { get; set; }
    public ICollection<RolesClaims> RoleClaims { get; set; } = new List<RolesClaims>();
    public ICollection<UsersRoles> UsersRoles { get; set; } = new List<UsersRoles>();
    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
}

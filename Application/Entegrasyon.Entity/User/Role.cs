using System.Text.Json.Serialization;
using Shared.Entity;
using Shared.User;

namespace Entegrasyon.Entity.User;

public class Role : BaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string NormalizedName { get; set; }
    public ICollection<RolesClaims> RoleClaims { get; set; }
    public ICollection<UsersRoles> UsersRoles { get; set; }
    public ICollection<ApplicationUser> Users { get; set; } = [];
    public ICollection<ApplicationClaim> Claims { get; set; } = [];
}
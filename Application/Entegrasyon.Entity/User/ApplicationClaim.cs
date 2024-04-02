using System.Text.Json.Serialization;
using Shared.Entity;

namespace Entegrasyon.Entity.User;
public class ApplicationClaim : BaseEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public ICollection<RolesClaims> RolesClaims { get; set; }
    public ICollection<Role> Roles { get; set; } = [];
    public ICollection<UsersClaims> UsersClaims { get; set; } = [];
    public ICollection<ApplicationUser> Users { get; set; } = [];
}
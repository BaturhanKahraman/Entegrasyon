using System.ComponentModel.DataAnnotations;

namespace Entegrasyon.Entity.User;
/// <summary>
/// Many to many table for role and permissions (using string-based permission names instead of IDs)
/// </summary>
public class RolesClaims
{
    public int RoleId { get; set; }
    public Role Role { get; set; }
    public int? ApplicationClaimId { get; set; }  // Legacy - deprecated in favor of Permission
    public string? Permission { get; set; }  // New: string-based permission name (e.g., "Permissions.Users.View")
}

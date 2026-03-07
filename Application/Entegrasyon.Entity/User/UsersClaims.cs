namespace Entegrasyon.Entity.User;

public class UsersClaims
{
    public Guid ApplicationUserId { get; set; }
    public ApplicationUser ApplicationUser { get; set; } = null!;
    public int? ApplicationClaimId { get; set; }  // Legacy - deprecated in favor of Permission
    public string? Permission { get; set; }  // New: string-based permission name (e.g., "Permissions.Users.View")
}

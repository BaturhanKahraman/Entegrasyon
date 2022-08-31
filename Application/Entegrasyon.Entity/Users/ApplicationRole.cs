using Shared.Abstract.Entity;

namespace Entegrasyon.Entity.Users;

public class ApplicationRole:ApplicationEntity
{
    public string Name { get; set; }
    public List<ApplicationUser> ApplicationUsers { get; set; }
    public List<ApplicationClaim> ApplicationClaims { get; set; }
}
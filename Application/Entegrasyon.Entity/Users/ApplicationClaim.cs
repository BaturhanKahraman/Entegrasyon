using Shared.Abstract.Entity;
using System.ComponentModel.DataAnnotations;
using System.Security.AccessControl;

namespace Entegrasyon.Entity.Users;

public class ApplicationClaim : ApplicationEntity
{
    [Required, StringLength(maximumLength: 55,MinimumLength = 3)]
    public string Name { get; set; }
    [StringLength(maximumLength: 255)]
    public string Description { get; set; }
    public List<ApplicationUser> Users { get; set; }
    public List<ApplicationRole> ApplicationRoles { get; set; }
}
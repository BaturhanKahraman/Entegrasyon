using Shared.Entity;
using System.ComponentModel.DataAnnotations;
using System.Security.AccessControl;

namespace Shared.User;

public class RootClaim : ApplicationEntity
{
    [Required, StringLength(maximumLength: 55,MinimumLength = 3)]
    public string Name { get; set; }
    [StringLength(maximumLength: 255)]
    public string Description { get; set; }
    public List<RootUser> Users { get; set; }
    public List<RootRole> Roles { get; set; }
}
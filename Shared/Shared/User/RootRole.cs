using System.ComponentModel.DataAnnotations.Schema;
using Shared.Entity;

namespace Shared.User;

[Table("Roles")]
public class RootRole : ApplicationEntity
{
    public string Name { get; set; }
    public List<RootUser> Users { get; set; }
    public virtual List<RootClaim> Claims { get; set; }
}
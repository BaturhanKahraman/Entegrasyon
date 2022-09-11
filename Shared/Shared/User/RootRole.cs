using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Shared.Entity;

namespace Shared.User;

[Table("Roles")]
public class RootRole : ApplicationEntity
{
    public string Name { get; set; }
    [JsonIgnore]
    public List<RootUser> Users { get; set; }
    public virtual List<RootClaim> Claims { get; set; }
}
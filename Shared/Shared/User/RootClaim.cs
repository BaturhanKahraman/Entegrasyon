using Shared.Entity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.AccessControl;
using System.Text.Json.Serialization;

namespace Shared.User;
[Table("Claims")]
public class RootClaim : BaseEntity
{
    public int Id { get; set; }
    [Required, StringLength(maximumLength: 55,MinimumLength = 3)]
    public string Name { get; set; }
    [StringLength(maximumLength: 255)]
    public string Description { get; set; }
    [JsonIgnore]
    public List<RootRole> Roles { get; set; }
}
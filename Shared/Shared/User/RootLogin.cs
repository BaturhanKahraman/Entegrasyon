using System.ComponentModel.DataAnnotations.Schema;
using Shared.Entity;

namespace Shared.User;

[Table("Logins")]
public class RootLogin:BaseEntity
{
    public Guid Id { get; set; }
    public DateTimeOffset LoginTime { get; set; }
    public string IpAddress { get; set; }


    public Guid RootUserId { get; set; }
    public RootUser RootUser { get; set; }
}
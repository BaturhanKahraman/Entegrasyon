using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Entegrasyon.Entity.User;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Entity.Logs;
[Index("LogAction")]
[Index("LogAction","LogType")]
[Index("EntityType", "EntityId")]
public sealed class ApplicationLog : BaseEntity
{
    public long Id { get; set; }
    public string? Content { get; set; }
    public Guid? ApplicationUserId { get; set; }
    public ApplicationUser? ApplicationUser { get; set; }
    public LogType LogType { get; set; }
    public LogAction LogAction { get; set; }
    public string? IpAddress { get; set; }
    [Column(TypeName = "jsonb")]
    public string? Object { get; set; }

    [StringLength(50)]
    public string? EntityType { get; set; }
    [StringLength(100)]
    public string? EntityId { get; set; }
}
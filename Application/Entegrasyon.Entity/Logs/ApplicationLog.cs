using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Shared.Entity;

namespace Entegrasyon.Entity.Logs;
[Index("LogAction")]
[Index("LogAction","LogType")]
public sealed class ApplicationLog : BaseEntity
{
    public long Id { get; set; }
    public string Content { get; set; }
    public Guid? ApplicationUserId { get; set; }
    public ApplicationUser ApplicationUser { get; set; }
    public LogType LogType { get; set; }
    public LogAction LogAction { get; set; }
    public string IpAddress { get; set; }
    [Column(TypeName = "jsonb")]
    public string Object { get; set; }
}
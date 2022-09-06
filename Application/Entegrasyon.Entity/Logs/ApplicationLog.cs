using Shared.Entity;

namespace Entegrasyon.Entity.Logs;

public class ApplicationLog : LongEntity
{
    public string Content { get; set; }
    public Guid? ApplicationUserId { get; set; }
    public ApplicationUser ApplicationUser { get; set; }
    public LogType LogType { get; set; }
    public LogAction LogAction { get; set; }
    public string IpAddress { get; set; }
}
using System.ComponentModel.DataAnnotations;
using Shared.Entity;

namespace Entegrasyon.Entity;

public sealed class Notification : BaseEntity
{
    public long Id { get; set; }
    [MaxLength(60)]
    public string Header { get; set; }
    public string Content { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset ReadDate { get; set; }
}
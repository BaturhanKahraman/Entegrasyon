using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Abstract.Entity;

namespace Entegrasyon.Entity;

public class Notification : LongEntity
{
    [MaxLength(60)]
    [Column(TypeName = "varchar")]
    public string Header { get; set; }
    [MaxLength(255)]
    [Column(TypeName = "varchar")]
    public string Content { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset ReadDate { get; set; }
}
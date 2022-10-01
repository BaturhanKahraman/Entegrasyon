using System.ComponentModel.DataAnnotations;

namespace Shared.Entity;

public interface IEntity<T>
where T : struct
{
    [Key]
    public T Id { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
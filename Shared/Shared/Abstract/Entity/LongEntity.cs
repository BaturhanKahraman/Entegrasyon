namespace Shared.Abstract.Entity;

public class LongEntity : IEntity<long>
{
    public long Id { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
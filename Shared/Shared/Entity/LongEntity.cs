namespace Shared.Entity;

public class LongEntity : IEntity<long>
{
    public long Id { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }= DateTimeOffset.Now;
}
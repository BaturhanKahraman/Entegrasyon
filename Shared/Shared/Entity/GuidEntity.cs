namespace Shared.Entity;

public class GuidEntity : IEntity<Guid>
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public bool IsDeleted { get; set; } = false;
    public DateTimeOffset CreatedAt { get; set; } = DateTime.Now;
}
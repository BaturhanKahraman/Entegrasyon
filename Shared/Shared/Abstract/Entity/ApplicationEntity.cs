namespace Shared.Abstract.Entity;

public class ApplicationEntity : IEntity<int>
{
    public int Id { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
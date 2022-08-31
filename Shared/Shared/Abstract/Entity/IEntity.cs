namespace Shared.Abstract.Entity;

public interface IEntity<T>
where T : struct
{
    public T Id { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
namespace Shared.Entity;

public class BaseEntity
{
    public bool IsDeleted { get; set; }
    public DateTimeOffset DeletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
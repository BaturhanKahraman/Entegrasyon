namespace Entegrasyon.Business.Channels.Events.Categories;

public sealed class CategoryDeletedEvent : BaseEvent
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid DeletedByUserId { get; set; }

    public CategoryDeletedEvent() { }
    public CategoryDeletedEvent(int categoryId, string name, Guid deletedByUserId)
    {
        CategoryId = categoryId;
        Name = name;
        DeletedByUserId = deletedByUserId;
    }
}

namespace Entegrasyon.Business.Channels.Events.Categories;

public sealed class CategoryAddedEvent : BaseEvent
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public Guid CreatedByUserId { get; set; }

    public CategoryAddedEvent() { }
    public CategoryAddedEvent(int categoryId, string name, int? parentId, Guid createdByUserId)
    {
        CategoryId = categoryId;
        Name = name;
        ParentId = parentId;
        CreatedByUserId = createdByUserId;
    }
}

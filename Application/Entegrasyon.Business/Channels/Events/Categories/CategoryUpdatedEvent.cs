namespace Entegrasyon.Business.Channels.Events.Categories;

public sealed class CategoryUpdatedEvent : BaseEvent
{
    public int CategoryId { get; set; }
    public string Action { get; set; } = string.Empty;
    public Guid UpdatedByUserId { get; set; }

    public CategoryUpdatedEvent() { }
    public CategoryUpdatedEvent(int categoryId, string action)
    {
        CategoryId = categoryId;
        Action = action;
    }

    public CategoryUpdatedEvent(int categoryId, string action, Guid updatedByUserId)
    {
        CategoryId = categoryId;
        Action = action;
        UpdatedByUserId = updatedByUserId;
    }
}

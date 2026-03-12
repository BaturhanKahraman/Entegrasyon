namespace Entegrasyon.Business.Channels.Events.Categories;

public class CategoryUpdatedEvent : BaseEvent
{
    public int CategoryId { get; set; }
    public string Action { get; set; } = string.Empty;

    public CategoryUpdatedEvent() { }
    public CategoryUpdatedEvent(int categoryId, string action)
    {
        CategoryId = categoryId;
        Action = action;
    }
}

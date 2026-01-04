namespace Entegrasyon.Blazor.Services.Channels.Events;

/// <summary>
/// Base class for all events
/// </summary>
public abstract class BaseEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Event raised when a product is created or updated
/// </summary>
public class ProductUpdatedEvent : BaseEvent
{
    public int ProductId { get; set; }
    public string Action { get; set; } = string.Empty; // "Created", "Updated", "Deleted"

    public ProductUpdatedEvent() { }

    public ProductUpdatedEvent(int productId, string action)
    {
        ProductId = productId;
        Action = action;
    }
}

/// <summary>
/// Event raised when a category is modified
/// </summary>
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

/// <summary>
/// Event raised when an order is created
/// </summary>
public class OrderCreatedEvent : BaseEvent
{
    public int OrderId { get; set; }
    public string MarketplaceName { get; set; } = string.Empty;

    public OrderCreatedEvent() { }

    public OrderCreatedEvent(int orderId, string marketplaceName)
    {
        OrderId = orderId;
        MarketplaceName = marketplaceName;
    }
}

/// <summary>
/// Event raised when marketplace sync is needed
/// </summary>
public class MarketplaceSyncEvent : BaseEvent
{
    public string MarketplaceName { get; set; } = string.Empty;
    public string SyncType { get; set; } = string.Empty; // "Product", "Category", "Order", etc.
    public int? EntityId { get; set; }

    public MarketplaceSyncEvent() { }

    public MarketplaceSyncEvent(string marketplaceName, string syncType, int? entityId = null)
    {
        MarketplaceName = marketplaceName;
        SyncType = syncType;
        EntityId = entityId;
    }
}

using Entegrasyon.Business.Abstract;

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

/// <summary>
/// Event raised when a notification is created
/// </summary>
public class NotificationEvent : BaseEvent
{
    public long NotificationId { get; set; }
    public string Header { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public IEnumerable<Guid> UserIds { get; set; } = Enumerable.Empty<Guid>();

    public NotificationEvent() { }

    public NotificationEvent(long notificationId, string header, string content, IEnumerable<Guid> userIds)
    {
        NotificationId = notificationId;
        Header = header;
        Content = content;
        UserIds = userIds;
    }
}

/// <summary>
/// Event raised when category import is requested
/// </summary>
public class CategoryImportRequestedEvent : BaseEvent
{
    public string MarketplaceName { get; set; } = string.Empty;
    public IEnumerable<ExternalCategoryImportRequest> Categories { get; set; } = Enumerable.Empty<ExternalCategoryImportRequest>();
    public Guid UserId { get; set; }

    public CategoryImportRequestedEvent() { }

    public CategoryImportRequestedEvent(string marketplaceName, IEnumerable<ExternalCategoryImportRequest> categories, Guid userId)
    {
        MarketplaceName = marketplaceName;
        Categories = categories;
        UserId = userId;
    }
}

/// <summary>
/// Event raised when category import is completed
/// </summary>
public class CategoryImportCompletedEvent : BaseEvent
{
    public string MarketplaceName { get; set; } = string.Empty;
    public int ImportedCount { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid UserId { get; set; }

    public CategoryImportCompletedEvent() { }

    public CategoryImportCompletedEvent(string marketplaceName, int importedCount, bool success, string? errorMessage, Guid userId)
    {
        MarketplaceName = marketplaceName;
        ImportedCount = importedCount;
        Success = success;
        ErrorMessage = errorMessage;
        UserId = userId;
    }
}

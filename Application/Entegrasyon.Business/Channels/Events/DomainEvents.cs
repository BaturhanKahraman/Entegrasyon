using Entegrasyon.Business.Abstract;

namespace Entegrasyon.Business.Channels.Events;

public abstract class BaseEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

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

public class ProductCreatedForMarketplaceEvent : BaseEvent
{
    public Guid ProductId { get; set; }
    public List<string> Marketplaces { get; set; } = new();

    public ProductCreatedForMarketplaceEvent() { }
    public ProductCreatedForMarketplaceEvent(Guid productId, IEnumerable<string> marketplaces)
    {
        ProductId = productId;
        Marketplaces = [..marketplaces];
    }
}

public class CategoryImportRequestedEvent : BaseEvent
{
    public string MarketplaceName { get; set; } = string.Empty;
    public IEnumerable<ExternalCategoryImportRequest> Categories { get; set; } = [];
    public Guid UserId { get; set; }

    public CategoryImportRequestedEvent() { }
    public CategoryImportRequestedEvent(string marketplaceName, IEnumerable<ExternalCategoryImportRequest> categories, Guid userId)
    {
        MarketplaceName = marketplaceName;
        Categories = categories;
        UserId = userId;
    }
}

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

public class NotificationEvent : BaseEvent
{
    public long NotificationId { get; set; }
    public string Header { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public IEnumerable<Guid> UserIds { get; set; } = [];

    public NotificationEvent() { }
    public NotificationEvent(long notificationId, string header, string content, IEnumerable<Guid> userIds)
    {
        NotificationId = notificationId;
        Header = header;
        Content = content;
        UserIds = userIds;
    }
}

namespace Entegrasyon.Business.Channels.Events.Categories;

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

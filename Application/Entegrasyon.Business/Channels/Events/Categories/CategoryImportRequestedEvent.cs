using Entegrasyon.Business.Abstract;

namespace Entegrasyon.Business.Channels.Events.Categories;

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

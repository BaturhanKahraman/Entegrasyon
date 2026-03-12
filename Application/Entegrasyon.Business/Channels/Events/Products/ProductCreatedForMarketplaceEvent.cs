namespace Entegrasyon.Business.Channels.Events.Products;

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

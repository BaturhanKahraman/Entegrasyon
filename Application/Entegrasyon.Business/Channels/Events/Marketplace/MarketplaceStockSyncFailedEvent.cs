namespace Entegrasyon.Business.Channels.Events.Marketplace;

public sealed class MarketplaceStockSyncFailedEvent : BaseEvent
{
    public int MarketPlaceId { get; set; }
    public Guid ProductId { get; set; }
    public string Error { get; set; } = string.Empty;

    public MarketplaceStockSyncFailedEvent() { }
    public MarketplaceStockSyncFailedEvent(int marketPlaceId, Guid productId, string error)
    {
        MarketPlaceId = marketPlaceId;
        ProductId = productId;
        Error = error;
    }
}

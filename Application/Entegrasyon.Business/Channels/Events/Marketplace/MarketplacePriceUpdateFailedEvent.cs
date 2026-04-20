namespace Entegrasyon.Business.Channels.Events.Marketplace;

public sealed class MarketplacePriceUpdateFailedEvent : BaseEvent
{
    public int MarketPlaceId { get; set; }
    public Guid ProductId { get; set; }
    public string Error { get; set; } = string.Empty;

    public MarketplacePriceUpdateFailedEvent() { }
    public MarketplacePriceUpdateFailedEvent(int marketPlaceId, Guid productId, string error)
    {
        MarketPlaceId = marketPlaceId;
        ProductId = productId;
        Error = error;
    }
}

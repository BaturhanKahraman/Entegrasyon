namespace Entegrasyon.Business.Channels.Events.Marketplace;

public sealed class MarketplaceProductApprovedEvent : BaseEvent
{
    public int MarketPlaceId { get; set; }
    public Guid ProductId { get; set; }
    public string MarketplaceProductCode { get; set; } = string.Empty;

    public MarketplaceProductApprovedEvent() { }
    public MarketplaceProductApprovedEvent(int marketPlaceId, Guid productId, string marketplaceProductCode)
    {
        MarketPlaceId = marketPlaceId;
        ProductId = productId;
        MarketplaceProductCode = marketplaceProductCode;
    }
}

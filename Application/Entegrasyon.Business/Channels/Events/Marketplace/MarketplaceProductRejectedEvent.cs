namespace Entegrasyon.Business.Channels.Events.Marketplace;

public sealed class MarketplaceProductRejectedEvent : BaseEvent
{
    public int MarketPlaceId { get; set; }
    public Guid ProductId { get; set; }
    public string RejectionReason { get; set; } = string.Empty;

    public MarketplaceProductRejectedEvent() { }
    public MarketplaceProductRejectedEvent(int marketPlaceId, Guid productId, string rejectionReason)
    {
        MarketPlaceId = marketPlaceId;
        ProductId = productId;
        RejectionReason = rejectionReason;
    }
}

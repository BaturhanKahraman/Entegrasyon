namespace Entegrasyon.Business.Channels.Events.Marketplace;

public sealed class MarketplaceReturnReceivedEvent : BaseEvent
{
    public int MarketPlaceId { get; set; }
    public long ReturnId { get; set; }
    public long OrderId { get; set; }
    public string Reason { get; set; } = string.Empty;

    public MarketplaceReturnReceivedEvent() { }
    public MarketplaceReturnReceivedEvent(int marketPlaceId, long returnId, long orderId, string reason)
    {
        MarketPlaceId = marketPlaceId;
        ReturnId = returnId;
        OrderId = orderId;
        Reason = reason;
    }
}

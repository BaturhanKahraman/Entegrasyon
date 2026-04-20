namespace Entegrasyon.Business.Channels.Events.Marketplace;

public sealed class MarketplaceOrderReceivedEvent : BaseEvent
{
    public int MarketPlaceId { get; set; }
    public long OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    public MarketplaceOrderReceivedEvent() { }
    public MarketplaceOrderReceivedEvent(int marketPlaceId, long orderId, string orderNumber, string customerName, decimal amount)
    {
        MarketPlaceId = marketPlaceId;
        OrderId = orderId;
        OrderNumber = orderNumber;
        CustomerName = customerName;
        Amount = amount;
    }
}

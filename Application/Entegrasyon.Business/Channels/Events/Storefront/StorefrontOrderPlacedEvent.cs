namespace Entegrasyon.Business.Channels.Events.Storefront;

public sealed class StorefrontOrderPlacedEvent : BaseEvent
{
    public long OrderId { get; set; }
    public int CustomerId { get; set; }
    public decimal Total { get; set; }

    public StorefrontOrderPlacedEvent() { }
    public StorefrontOrderPlacedEvent(long orderId, int customerId, decimal total)
    {
        OrderId = orderId;
        CustomerId = customerId;
        Total = total;
    }
}

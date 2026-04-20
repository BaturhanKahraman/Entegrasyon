namespace Entegrasyon.Business.Channels.Events.Storefront;

public sealed class StorefrontAbandonedCartEvent : BaseEvent
{
    public long CartId { get; set; }
    public int CustomerId { get; set; }
    public decimal ValueAmount { get; set; }

    public StorefrontAbandonedCartEvent() { }
    public StorefrontAbandonedCartEvent(long cartId, int customerId, decimal valueAmount)
    {
        CartId = cartId;
        CustomerId = customerId;
        ValueAmount = valueAmount;
    }
}

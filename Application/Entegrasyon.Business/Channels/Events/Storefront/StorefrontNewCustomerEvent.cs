namespace Entegrasyon.Business.Channels.Events.Storefront;

public sealed class StorefrontNewCustomerEvent : BaseEvent
{
    public int CustomerId { get; set; }
    public string Email { get; set; } = string.Empty;

    public StorefrontNewCustomerEvent() { }
    public StorefrontNewCustomerEvent(int customerId, string email)
    {
        CustomerId = customerId;
        Email = email;
    }
}

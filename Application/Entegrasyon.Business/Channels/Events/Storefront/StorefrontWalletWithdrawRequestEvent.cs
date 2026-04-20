namespace Entegrasyon.Business.Channels.Events.Storefront;

public sealed class StorefrontWalletWithdrawRequestEvent : BaseEvent
{
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }

    public StorefrontWalletWithdrawRequestEvent() { }
    public StorefrontWalletWithdrawRequestEvent(int customerId, decimal amount)
    {
        CustomerId = customerId;
        Amount = amount;
    }
}

namespace Marketplace
{
    public interface IMarketPlaceOrderService
    {
        Task CheckNewOrder();
        Task GetOrders();
        Task AbortOrder();

    }
}
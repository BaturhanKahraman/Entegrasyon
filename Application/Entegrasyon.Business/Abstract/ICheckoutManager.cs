using Entegrasyon.Entity.Dtos.Storefront;
using Entegrasyon.Entity.Orders;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface ICheckoutManager
{
    Task<IDataResult<Order>> CreateOrderFromCartAsync(
        Guid cartId, int customerId, int tenantId,
        CheckoutRequestDto dto, decimal freeShippingThreshold, decimal flatShippingRate);

    Task<IResult> CompleteOrderPaymentAsync(Guid orderId, string transactionId, decimal paidAmount);
    Task<IResult> FailOrderPaymentAsync(Guid orderId, string? errorMessage);
}

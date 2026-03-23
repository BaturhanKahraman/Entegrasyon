using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Pazarama;

/// <summary>
/// Pazarama sipariş servisi — mock implementasyon (test/geliştirme ortamı).
/// Gerçek API çağrısı yapmaz; fetch için boş liste, güncellemeler için başarı döner.
/// </summary>
public sealed class MockPazaramaOrderService(
    ILogger<MockPazaramaOrderService> logger) : IPazaramaOrderService
{
    public Task<IDataResult<List<PazaramaOrderDto>>> FetchOrdersAsync(
        DateTimeOffset startDate, DateTimeOffset endDate,
        int pageSize = 500, int pageNumber = 1)
    {
        logger.LogInformation("MockPazarama: FetchOrders {Start} - {End} (pageSize={PageSize}, pageNumber={PageNumber})",
            startDate, endDate, pageSize, pageNumber);
        return Task.FromResult<IDataResult<List<PazaramaOrderDto>>>(
            new SuccessDataResult<List<PazaramaOrderDto>>(new List<PazaramaOrderDto>()));
    }

    public Task<IResult> UpdateOrderItemStatusAsync(long orderNumber, PazaramaOrderItemUpdate item)
    {
        logger.LogInformation("MockPazarama: UpdateOrderItemStatus OrderNumber={OrderNumber}, ItemId={ItemId}, Status={Status}",
            orderNumber, item.OrderItemId, item.Status);
        return Task.FromResult<IResult>(new SuccessResult());
    }

    public Task<IResult> BulkUpdateOrderStatusAsync(long orderNumber, int status)
    {
        logger.LogInformation("MockPazarama: BulkUpdateOrderStatus OrderNumber={OrderNumber}, Status={Status}",
            orderNumber, status);
        return Task.FromResult<IResult>(new SuccessResult());
    }
}

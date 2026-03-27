using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Amazon;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Amazon;

public sealed class MockAmazonOrderService(ILogger<MockAmazonOrderService> logger) : IAmazonOrderService
{
    public Task<IDataResult<List<AmazonOrderDto>>> GetOrdersAsync(
        DateTimeOffset createdAfter, string[] marketplaceIds,
        string[]? orderStatuses = null, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Amazon get orders since {After}", createdAfter);
        return Task.FromResult<IDataResult<List<AmazonOrderDto>>>(new SuccessDataResult<List<AmazonOrderDto>>([]));
    }

    public Task<IDataResult<AmazonOrderDto>> GetOrderAsync(string orderId, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Amazon get order: {OrderId}", orderId);
        return Task.FromResult<IDataResult<AmazonOrderDto>>(new ErrorDataResult<AmazonOrderDto>(null!, "Mock: order not found."));
    }

    public Task<IDataResult<List<AmazonOrderItemDto>>> GetOrderItemsAsync(string orderId, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Amazon get order items: {OrderId}", orderId);
        return Task.FromResult<IDataResult<List<AmazonOrderItemDto>>>(new SuccessDataResult<List<AmazonOrderItemDto>>([]));
    }

    public Task<IResult> ConfirmShipmentAsync(string orderId, AmazonConfirmShipmentRequest request, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] Amazon confirm shipment: {OrderId}", orderId);
        return Task.FromResult<IResult>(new SuccessResult("Kargo onaylandı (MOCK)."));
    }
}

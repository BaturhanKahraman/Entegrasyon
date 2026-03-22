using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.N11;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.N11;

/// <summary>
/// Mock N11 sipariş servisi — boş liste döner.
/// Gerçek SOAP bağlantısı olmadan UI geliştirmesi için kullanılır.
/// </summary>
public sealed class MockN11OrderService(
    ILogger<MockN11OrderService> logger) : IN11OrderService
{
    public Task<IDataResult<List<N11OrderDto>>> FetchOrdersAsync(
        DateTimeOffset? startDate = null, DateTimeOffset? endDate = null,
        string? status = null, int page = 0, int pageSize = 50)
    {
        logger.LogInformation(
            "Mock N11: FetchOrdersAsync çağrıldı — startDate={StartDate}, endDate={EndDate}, status={Status}, page={Page}, pageSize={PageSize}",
            startDate, endDate, status, page, pageSize);

        return Task.FromResult<IDataResult<List<N11OrderDto>>>(
            new SuccessDataResult<List<N11OrderDto>>([], "N11 sipariş listesi (mock — boş)."));
    }

    public Task<IDataResult<N11OrderDto>> GetOrderDetailAsync(long orderId)
    {
        logger.LogInformation("Mock N11: GetOrderDetailAsync çağrıldı — orderId={OrderId}", orderId);

        return Task.FromResult<IDataResult<N11OrderDto>>(
            new ErrorDataResult<N11OrderDto>(null!, $"Mock: sipariş {orderId} bulunamadı."));
    }
}

using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Hepsiburada;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Hepsiburada;

/// <summary>
/// Mock Hepsiburada sipariş servisi — development ve test için.
/// </summary>
public sealed class MockHepsiburadaOrderService(
    ILogger<MockHepsiburadaOrderService> logger) : IHepsiburadaOrderService
{
    public Task<IDataResult<List<HepsiburadaOrderDto>>> GetOrdersAsync(
        DateTimeOffset? beginDate = null, DateTimeOffset? endDate = null,
        int offset = 0, int limit = 50)
    {
        logger.LogInformation("[MOCK] HB get orders");
        return Task.FromResult<IDataResult<List<HepsiburadaOrderDto>>>(
            new SuccessDataResult<List<HepsiburadaOrderDto>>([]));
    }

    public Task<IDataResult<HepsiburadaOrderDto>> GetOrderAsync(string orderNumber)
    {
        logger.LogInformation("[MOCK] HB get order: {OrderNumber}", orderNumber);
        return Task.FromResult<IDataResult<HepsiburadaOrderDto>>(
            new ErrorDataResult<HepsiburadaOrderDto>(null!, "Mock: sipariş bulunamadı."));
    }

    public Task<IDataResult<HepsiburadaPackageResponse>> CreatePackageAsync(HepsiburadaPackageRequest request)
    {
        logger.LogInformation("[MOCK] HB create package: {ItemCount} items", request.LineItemRequests.Count);
        var response = new HepsiburadaPackageResponse($"MOCK-PKG-{Guid.NewGuid():N}"[..20], "MOCK-BARCODE");
        return Task.FromResult<IDataResult<HepsiburadaPackageResponse>>(
            new SuccessDataResult<HepsiburadaPackageResponse>(response));
    }

    public Task<IResult> CancelLineItemAsync(string lineItemId, int reasonId)
    {
        logger.LogInformation("[MOCK] HB cancel line item: {LineItemId}", lineItemId);
        return Task.FromResult<IResult>(new SuccessResult("Sipariş kalemi iptal edildi (MOCK)."));
    }

    public Task<IResult> AddInvoiceAsync(string lineItemId, HepsiburadaInvoiceRequest invoice)
    {
        logger.LogInformation("[MOCK] HB add invoice: {LineItemId}", lineItemId);
        return Task.FromResult<IResult>(new SuccessResult("Fatura eklendi (MOCK)."));
    }
}

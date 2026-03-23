using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Pazarama;

/// <summary>
/// Pazarama fatura servisi — mock implementasyon (test/geliştirme ortamı).
/// Gerçek API çağrısı yapmaz; her zaman başarı döner.
/// </summary>
public sealed class MockPazaramaInvoiceService(
    ILogger<MockPazaramaInvoiceService> logger) : IPazaramaInvoiceService
{
    public Task<IResult> UploadInvoiceLinkAsync(PazaramaInvoiceLinkRequest request)
    {
        logger.LogInformation("MockPazarama: UploadInvoiceLink OrderId={OrderId}", request.OrderId);
        return Task.FromResult<IResult>(new SuccessResult());
    }

    public Task<IResult> UploadMultipleInvoiceLinkAsync(PazaramaMultipleInvoiceLinkRequest request)
    {
        logger.LogInformation("MockPazarama: UploadMultipleInvoiceLink OrderId={OrderId}, ItemCount={Count}",
            request.OrderId, request.OrderItemIds?.Count ?? 0);
        return Task.FromResult<IResult>(new SuccessResult());
    }
}

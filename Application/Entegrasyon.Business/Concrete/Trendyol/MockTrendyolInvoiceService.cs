using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Trendyol;

/// <summary>
/// Mock fatura servisi — logla, 201 dön.
/// </summary>
public sealed class MockTrendyolInvoiceService(
    ILogger<MockTrendyolInvoiceService> logger) : ITrendyolInvoiceService
{
    public Task<IResult> SendInvoiceLinkAsync(long shipmentPackageId, string invoiceLink,
        long? invoiceDateTime = null, string? invoiceNumber = null)
    {
        logger.LogInformation("Mock: Invoice link sent for package {PackageId}: {Link}",
            shipmentPackageId, invoiceLink);
        return Task.FromResult<IResult>(new SuccessResult("Fatura linki gönderildi (mock)."));
    }

    public Task<IResult> DeleteInvoiceLinkAsync(long serviceSourceId, long customerId)
    {
        logger.LogInformation("Mock: Invoice link deleted for source {SourceId}, customer {CustomerId}",
            serviceSourceId, customerId);
        return Task.FromResult<IResult>(new SuccessResult("Fatura linki silindi (mock)."));
    }

    public Task<IResult> UploadInvoiceFileAsync(long shipmentPackageId, Stream file, string contentType)
    {
        logger.LogInformation("Mock: Invoice file uploaded for package {PackageId}, type={ContentType}",
            shipmentPackageId, contentType);
        return Task.FromResult<IResult>(new SuccessResult("Fatura dosyası yüklendi (mock)."));
    }
}

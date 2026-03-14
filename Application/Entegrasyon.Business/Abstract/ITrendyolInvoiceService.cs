using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface ITrendyolInvoiceService
{
    Task<IResult> SendInvoiceLinkAsync(long shipmentPackageId, string invoiceLink, long? invoiceDateTime = null, string? invoiceNumber = null);
    Task<IResult> DeleteInvoiceLinkAsync(long serviceSourceId, long customerId);
    Task<IResult> UploadInvoiceFileAsync(long shipmentPackageId, Stream file, string contentType);
}

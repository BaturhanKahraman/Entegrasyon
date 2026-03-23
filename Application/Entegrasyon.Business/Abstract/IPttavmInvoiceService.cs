using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IPttavmInvoiceService
{
    Task<IResult> SendInvoiceAsync(string orderId, List<int> lineItemIds, string? pdfUrl, string? base64Content, CancellationToken ct = default);
}

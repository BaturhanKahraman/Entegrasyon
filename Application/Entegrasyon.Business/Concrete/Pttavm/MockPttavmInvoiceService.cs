using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Pttavm;

/// <summary>
/// Mock PttAVM fatura servisi — development ve test ortamlari icin.
/// </summary>
public sealed class MockPttavmInvoiceService(
    ILogger<MockPttavmInvoiceService> logger) : IPttavmInvoiceService
{
    public Task<IResult> SendInvoiceAsync(
        string orderId, List<int> lineItemIds, string? pdfUrl, string? base64Content,
        CancellationToken ct = default)
    {
        logger.LogDebug("[MOCK] PttAVM SendInvoice: orderId={OrderId}, {Count} items", orderId, lineItemIds.Count);
        return Task.FromResult<IResult>(new SuccessResult("Mock: Fatura gönderildi."));
    }
}

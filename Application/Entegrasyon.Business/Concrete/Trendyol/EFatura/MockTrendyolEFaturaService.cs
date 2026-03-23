using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Invoices;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Trendyol.EFatura;

/// <summary>
/// Mock e-Fatura servisi — dev/test icin.
/// </summary>
public sealed class MockTrendyolEFaturaService(
    ILogger<MockTrendyolEFaturaService> logger) : ITrendyolEFaturaService
{
    public Task<IDataResult<bool>> CheckTaxPayerAsync(string taxId, CancellationToken ct = default)
    {
        logger.LogInformation("Mock: CheckTaxPayer for {TaxId}", taxId);
        // 10 haneli VKN → mukellef, 11 haneli TCKN → degil (mock logic)
        var isTaxPayer = taxId.Length == 10;
        return Task.FromResult<IDataResult<bool>>(new SuccessDataResult<bool>(isTaxPayer));
    }

    public Task<IDataResult<EFaturaRecord>> CreateInvoiceForOrderAsync(Guid orderId, CancellationToken ct = default)
    {
        logger.LogInformation("Mock: CreateInvoiceForOrder {OrderId}", orderId);
        var record = new EFaturaRecord
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            InvoiceUuid = Guid.NewGuid().ToString(),
            InvoiceId = "MCK2025000000001",
            InvoiceType = EFaturaType.EArchive,
            Status = EFaturaStatus.Processing,
            ApiStatusCode = 10,
            PayableAmountKurus = 11455,
            TaxAmountKurus = 1755,
            LocalReferenceId = $"ORDER-{orderId}"
        };
        return Task.FromResult<IDataResult<EFaturaRecord>>(new SuccessDataResult<EFaturaRecord>(record));
    }

    public Task<IDataResult<EFaturaRecord>> CheckInvoiceStatusAsync(Guid invoiceRecordId, CancellationToken ct = default)
    {
        logger.LogInformation("Mock: CheckInvoiceStatus {RecordId}", invoiceRecordId);
        var record = new EFaturaRecord
        {
            Id = invoiceRecordId,
            Status = EFaturaStatus.Approved,
            ApiStatusCode = 205,
            ApprovedAt = DateTimeOffset.UtcNow
        };
        return Task.FromResult<IDataResult<EFaturaRecord>>(new SuccessDataResult<EFaturaRecord>(record));
    }

    public Task<IResult> CancelInvoiceAsync(Guid invoiceRecordId, CancellationToken ct = default)
    {
        logger.LogInformation("Mock: CancelInvoice {RecordId}", invoiceRecordId);
        return Task.FromResult<IResult>(new SuccessResult("Fatura iptal edildi (mock)."));
    }

    public Task<IDataResult<string>> GetInvoicePdfUrlAsync(Guid invoiceRecordId, CancellationToken ct = default)
    {
        logger.LogInformation("Mock: GetInvoicePdfUrl {RecordId}", invoiceRecordId);
        return Task.FromResult<IDataResult<string>>(
            new SuccessDataResult<string>("https://mock-pdf.example.com/invoice.pdf"));
    }

    public Task<IDataResult<int>> GetRemainingCreditsAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Mock: GetRemainingCredits");
        return Task.FromResult<IDataResult<int>>(new SuccessDataResult<int>(100));
    }
}

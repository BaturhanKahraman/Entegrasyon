using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Invoicing;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Invoicing;

/// <summary>
/// Mock e-fatura entegrator istemcisi. Test ve development ortami icin.
/// Gercek API cagirisi yapmaz, basarili sonuc doner.
/// </summary>
public sealed class MockInvoiceClient(
    ILogger<MockInvoiceClient> logger) : IEInvoiceIntegratorClient
{
    public Task<IDataResult<string>> SendInvoice(EInvoice invoice, CancellationToken ct = default)
    {
        var gibUuid = Guid.NewGuid().ToString();
        logger.LogInformation("[MOCK] E-Fatura gonderildi: {InvoiceNumber} -> GIB UUID: {GibUuid}",
            invoice.InvoiceNumber, gibUuid);

        return Task.FromResult<IDataResult<string>>(
            new SuccessDataResult<string>(gibUuid, "Fatura basariyla gonderildi. (Mock)"));
    }

    public Task<IResult> CancelInvoice(string gibUuid, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] E-Fatura iptal edildi: {GibUuid}", gibUuid);
        return Task.FromResult<IResult>(new SuccessResult("Fatura basariyla iptal edildi. (Mock)"));
    }

    public Task<IDataResult<EInvoiceStatus>> GetStatus(string gibUuid, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] E-Fatura durumu soruldu: {GibUuid}", gibUuid);
        return Task.FromResult<IDataResult<EInvoiceStatus>>(
            new SuccessDataResult<EInvoiceStatus>(EInvoiceStatus.Accepted));
    }

    public Task<IDataResult<byte[]>> DownloadPdf(string gibUuid, CancellationToken ct = default)
    {
        logger.LogInformation("[MOCK] E-Fatura PDF indirildi: {GibUuid}", gibUuid);
        var mockPdf = System.Text.Encoding.UTF8.GetBytes($"%PDF-1.4 Mock Invoice PDF for {gibUuid}");
        return Task.FromResult<IDataResult<byte[]>>(
            new SuccessDataResult<byte[]>(mockPdf));
    }
}

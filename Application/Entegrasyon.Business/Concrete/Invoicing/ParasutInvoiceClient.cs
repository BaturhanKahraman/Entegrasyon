using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Invoicing;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete.Invoicing;

/// <summary>
/// Parasut API v4 uzerinden e-fatura islemleri.
/// Gercek implementasyon ileride OAuth2 token management ile tamamlanacak.
/// </summary>
public sealed class ParasutInvoiceClient(
    ILogger<ParasutInvoiceClient> logger) : IEInvoiceIntegratorClient
{
    public Task<IDataResult<string>> SendInvoice(EInvoice invoice, CancellationToken ct = default)
    {
        logger.LogWarning("Parasut fatura gonderimi henuz implemente edilmedi. InvoiceNumber: {InvoiceNumber}",
            invoice.InvoiceNumber);
        return Task.FromResult<IDataResult<string>>(
            new ErrorDataResult<string>(string.Empty, "Parasut entegrasyonu henuz aktif degil."));
    }

    public Task<IResult> CancelInvoice(string gibUuid, CancellationToken ct = default)
    {
        logger.LogWarning("Parasut fatura iptali henuz implemente edilmedi. GibUuid: {GibUuid}", gibUuid);
        return Task.FromResult<IResult>(new ErrorResult("Parasut entegrasyonu henuz aktif degil."));
    }

    public Task<IDataResult<EInvoiceStatus>> GetStatus(string gibUuid, CancellationToken ct = default)
    {
        logger.LogWarning("Parasut durum sorgusu henuz implemente edilmedi. GibUuid: {GibUuid}", gibUuid);
        return Task.FromResult<IDataResult<EInvoiceStatus>>(
            new ErrorDataResult<EInvoiceStatus>(EInvoiceStatus.Draft, "Parasut entegrasyonu henuz aktif degil."));
    }

    public Task<IDataResult<byte[]>> DownloadPdf(string gibUuid, CancellationToken ct = default)
    {
        logger.LogWarning("Parasut PDF indirme henuz implemente edilmedi. GibUuid: {GibUuid}", gibUuid);
        return Task.FromResult<IDataResult<byte[]>>(
            new ErrorDataResult<byte[]>([], "Parasut entegrasyonu henuz aktif degil."));
    }
}

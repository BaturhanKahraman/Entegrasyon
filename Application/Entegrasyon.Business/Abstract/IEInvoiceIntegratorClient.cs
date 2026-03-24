using Entegrasyon.Entity.Invoicing;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IEInvoiceIntegratorClient
{
    Task<IDataResult<string>> SendInvoice(EInvoice invoice, CancellationToken ct = default);
    Task<IResult> CancelInvoice(string gibUuid, CancellationToken ct = default);
    Task<IDataResult<EInvoiceStatus>> GetStatus(string gibUuid, CancellationToken ct = default);
    Task<IDataResult<byte[]>> DownloadPdf(string gibUuid, CancellationToken ct = default);
}

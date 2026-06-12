using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Invoicing;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IEInvoiceManager
{
    Task<IDataResult<Guid>> CreateInvoice(CreateEInvoiceDto dto);
    Task<IDataResult<List<Guid>>> CreateBulkInvoices(BulkInvoiceDto dto);
    Task<IDataResult<Pageable<EInvoiceListDto>>> GetInvoices(EInvoiceFilterDto filter);
    Task<IDataResult<EInvoiceSummaryDto>> GetInvoiceSummary(EInvoiceFilterDto filter);
    Task<IDataResult<EInvoiceDetailDto>> GetInvoiceDetail(Guid invoiceId);
    Task<IResult> CancelInvoice(Guid invoiceId);
    Task<IResult> SendToGib(Guid invoiceId);
    Task<IDataResult<byte[]>> DownloadPdf(Guid invoiceId);
    Task<IDataResult<CreateEInvoiceDto>> GetInvoiceFromSale(Guid saleId);
}

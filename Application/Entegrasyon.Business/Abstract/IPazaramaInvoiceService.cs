using Entegrasyon.Business.Concrete.Pazarama;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IPazaramaInvoiceService
{
    /// <summary>
    /// Sipariş/paket/item bazlı fatura linki yükler.
    /// POST /order/invoice-link
    /// </summary>
    Task<IResult> UploadInvoiceLinkAsync(PazaramaInvoiceLinkRequest request);

    /// <summary>
    /// Birden fazla item'a fatura linki yükler.
    /// POST /order/multiple-invoice-link
    /// </summary>
    Task<IResult> UploadMultipleInvoiceLinkAsync(PazaramaMultipleInvoiceLinkRequest request);
}

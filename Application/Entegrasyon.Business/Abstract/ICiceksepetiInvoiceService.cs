using Entegrasyon.Business.Concrete.Ciceksepeti;
using Entegrasyon.Entity.Results;
namespace Entegrasyon.Business.Abstract;
public interface ICiceksepetiInvoiceService
{
    Task<IResult> SendInvoiceAsync(CiceksepetiInvoiceRequest request, CancellationToken ct = default);
}

using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Receipts;

namespace Entegrasyon.Business.Abstract;

public interface IReceiptRenderer
{
    Task<string> RenderAsync(SaleDetailDto sale, ReceiptMode mode, ReceiptSize size);
}

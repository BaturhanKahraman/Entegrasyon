using Entegrasyon.Entity.Dtos.Receipts;
using Entegrasyon.Entity.Dtos.Sale;
using Entegrasyon.Entity.Receipts;

namespace Entegrasyon.Business.Abstract;

public interface IReceiptRenderer
{
    Task<string> RenderAsync(SaleDetailDto sale, ReceiptMode mode, ReceiptSize size);
    string RenderWithTemplate(ReceiptTemplateDto template, SaleDetailDto sale, ReceiptMode mode, ReceiptSize size);
}

using Entegrasyon.PrintAgent.Contracts.Labels;

namespace Entegrasyon.Business.Labels;

public interface IReceiptGenerator
{
    byte[] GenerateSaleReceipt(SaleReceiptData data);
}

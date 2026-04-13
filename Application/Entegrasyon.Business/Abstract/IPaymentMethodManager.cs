using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Sales;

namespace Entegrasyon.Business.Abstract;

public interface IPaymentMethodManager
{
    Task<IDataResult<List<PaymentMethodDefinition>>> GetActivePaymentMethodsAsync(int tenantId);
    Task<IDataResult<List<PaymentMethodDefinition>>> GetAllPaymentMethodsAsync(int tenantId);
    Task<IResult> TogglePaymentMethodAsync(int id);
    Task<IResult> UpdatePaymentMethodAsync(int id, string name, string icon, decimal? commissionRate);
    Task<IResult> ReorderPaymentMethodsAsync(List<int> orderedIds);
    Task<IResult> CreatePaymentMethodAsync(string name, string systemCode, string icon, bool requiresAuthCode, bool requiresCashInput, int tenantId);
}

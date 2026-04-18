using Entegrasyon.Entity.Sales;

namespace Entegrasyon.Business.Abstract;

public interface IDiscountReasonManager
{
    Task<IReadOnlyList<DiscountReason>> GetActiveAsync(CancellationToken ct = default);
}

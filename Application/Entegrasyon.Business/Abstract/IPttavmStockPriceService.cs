using Entegrasyon.Business.Concrete.Pttavm;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IPttavmStockPriceService
{
    Task<IDataResult<PttavmUpsertResult>> UpdateStockPricesAsync(List<PttavmStockPriceRequest> items, CancellationToken ct = default);
    Task<IDataResult<List<PttavmProductInfo>>> SearchProductsAsync(PttavmProductSearchFilter filter, CancellationToken ct = default);
}

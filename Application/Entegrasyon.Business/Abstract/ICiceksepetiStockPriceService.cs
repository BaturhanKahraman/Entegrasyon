using Entegrasyon.Business.Concrete.Ciceksepeti;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface ICiceksepetiStockPriceService
{
    Task<IDataResult<List<string>>> UpdateStockAndPriceAsync(List<CiceksepetiStockPriceItem> items, CancellationToken ct = default);
}

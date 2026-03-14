using Entegrasyon.Entity.Dtos.Trendyol;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface ITrendyolStockPriceService
{
    Task<IDataResult<string>> UpdatePriceAndInventoryAsync(List<TrendyolPriceInventoryItem> items);
}

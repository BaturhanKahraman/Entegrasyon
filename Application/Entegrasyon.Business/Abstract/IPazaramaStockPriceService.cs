using Entegrasyon.Business.Concrete.Pazarama;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IPazaramaStockPriceService
{
    Task<IDataResult<string>> UpdateStockAsync(List<PazaramaStockUpdateItem> items);
    Task<IDataResult<string>> UpdatePriceAsync(List<PazaramaPriceUpdateItem> items);
}

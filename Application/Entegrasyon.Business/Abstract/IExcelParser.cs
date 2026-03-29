using Entegrasyon.Business.Concrete.BulkOperations;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IExcelParser
{
    IDataResult<List<ProductImportRow>> ParseProductImport(Stream stream);
    IDataResult<List<PriceImportRow>> ParsePriceImport(Stream stream);
    IDataResult<List<StockImportRow>> ParseStockImport(Stream stream);
}

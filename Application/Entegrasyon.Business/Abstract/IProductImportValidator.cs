using Entegrasyon.Business.Concrete.BulkOperations;
using Entegrasyon.Entity.Dtos.BulkOperations;

namespace Entegrasyon.Business.Abstract;

public interface IProductImportValidator
{
    List<BulkImportRowErrorDto> ValidateProductRows(List<ProductImportRow> rows);
    List<BulkImportRowErrorDto> ValidatePriceRows(List<PriceImportRow> rows);
    List<BulkImportRowErrorDto> ValidateStockRows(List<StockImportRow> rows);
}

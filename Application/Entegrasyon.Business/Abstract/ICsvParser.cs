using Entegrasyon.Business.Concrete.BulkOperations;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface ICsvParser
{
    IDataResult<List<ProductImportRow>> ParseProductImport(Stream stream);
    IDataResult<List<PriceImportRow>> ParsePriceImport(Stream stream);
    IDataResult<List<StockImportRow>> ParseStockImport(Stream stream);

    // Column mapping overloads
    IDataResult<List<ProductImportRow>> ParseProductImport(Stream stream, Dictionary<string, string> columnMapping);
    IDataResult<List<PriceImportRow>> ParsePriceImport(Stream stream, Dictionary<string, string> columnMapping);
    IDataResult<List<StockImportRow>> ParseStockImport(Stream stream, Dictionary<string, string> columnMapping);
    (List<string> Headers, List<List<string>> PreviewRows) ReadHeadersAndPreview(Stream stream, int previewRows = 5);
}

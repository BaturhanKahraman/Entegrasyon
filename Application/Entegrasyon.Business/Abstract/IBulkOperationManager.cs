using Entegrasyon.Entity.BulkOperations;
using Entegrasyon.Entity.Dtos.BulkOperations;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IBulkOperationManager
{
    Task<IDataResult<BulkImportResultDto>> ImportProductsAsync(Stream excelStream, string fileName, Guid userId);
    Task<IDataResult<BulkImportResultDto>> ImportPricesAsync(Stream excelStream, string fileName, Guid userId);
    Task<IDataResult<BulkImportResultDto>> ImportStockAsync(Stream excelStream, string fileName, Guid userId);
    Task<IDataResult<byte[]>> ExportProductsAsync(ExportFilterDto filter);
    Task<IDataResult<byte[]>> ExportPricesAsync(ExportFilterDto filter);
    Task<IDataResult<byte[]>> ExportStockAsync(ExportFilterDto filter);
    Task<IDataResult<byte[]>> GetImportTemplateAsync(BulkOperationType type);
    Task<IDataResult<BulkOperationLog>> GetOperationLogAsync(long id);
    Task<IDataResult<List<BulkOperationLog>>> GetRecentOperationsAsync(int count = 20);
}

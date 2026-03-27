using Entegrasyon.Entity.BulkOperations;
using Entegrasyon.Entity.Dtos.BulkOperations;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IBulkOperationManager
{
    Task<IDataResult<BulkImportResultDto>> ImportProductsAsync(Stream excelStream, string fileName, Guid userId, CancellationToken cancellationToken = default, IProgress<BulkOperationProgressDto>? progress = null);
    Task<IDataResult<BulkImportResultDto>> ImportPricesAsync(Stream excelStream, string fileName, Guid userId, CancellationToken cancellationToken = default, IProgress<BulkOperationProgressDto>? progress = null);
    Task<IDataResult<BulkImportResultDto>> ImportStockAsync(Stream excelStream, string fileName, Guid userId, CancellationToken cancellationToken = default, IProgress<BulkOperationProgressDto>? progress = null);
    Task<IDataResult<byte[]>> ExportProductsAsync(ExportFilterDto filter, CancellationToken cancellationToken = default);
    Task<IDataResult<byte[]>> ExportPricesAsync(ExportFilterDto filter, CancellationToken cancellationToken = default);
    Task<IDataResult<byte[]>> ExportStockAsync(ExportFilterDto filter, CancellationToken cancellationToken = default);
    Task<IDataResult<byte[]>> GetImportTemplateAsync(BulkOperationType type);
    Task<IDataResult<ImportValidationPreviewDto>> ValidateImportAsync(Stream excelStream, BulkOperationType operationType);
    Task<IDataResult<BulkOperationLog>> GetOperationLogAsync(long id);
    Task<IDataResult<List<BulkOperationLog>>> GetRecentOperationsAsync(int count = 20);
    Task<IDataResult<List<ExportColumnDto>>> GetAvailableColumnsAsync(BulkOperationType type);
}

using Entegrasyon.Business.Concrete.Ciceksepeti;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface ICiceksepetiProductService
{
    Task<IDataResult<string>> PublishProductAsync(Guid productId, CancellationToken ct = default);
    Task<IDataResult<string>> UpdateProductAsync(Guid productId, CancellationToken ct = default);
    Task<IDataResult<CiceksepetiBatchStatusResponse>> CheckBatchStatusAsync(string batchId, CancellationToken ct = default);
    Task<IDataResult<CiceksepetiProductListResponse>> GetProductsAsync(int page = 1, int pageSize = 60, int? statusFilter = null, CancellationToken ct = default);
}

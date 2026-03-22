using Entegrasyon.Business.Concrete.Pazarama;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IPazaramaProductService
{
    Task<IDataResult<string>> PublishProductAsync(Guid productId);
    Task<IDataResult<PazaramaBatchStatusResponse>> CheckBatchStatusAsync(string batchRequestId);
}

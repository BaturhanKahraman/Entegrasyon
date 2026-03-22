using Entegrasyon.Entity.Dtos.Amazon;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IAmazonProductService
{
    Task<IDataResult<string>> PublishProductAsync(Guid productId, CancellationToken ct = default);
    Task<IDataResult<AmazonListingItemResponse>> CheckListingStatusAsync(string sku, CancellationToken ct = default);
    Task<IResult> UpdateProductAsync(Guid productId, CancellationToken ct = default);
    Task<IResult> DeleteProductAsync(Guid productId, CancellationToken ct = default);
}

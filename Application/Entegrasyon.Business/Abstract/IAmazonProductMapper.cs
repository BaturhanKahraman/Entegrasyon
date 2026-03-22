using Entegrasyon.Entity.Dtos.Amazon;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IAmazonProductMapper
{
    Task<IDataResult<AmazonListingItem>> MapProductAsync(Guid productId, string productType, CancellationToken ct = default);
}

using Entegrasyon.Business.Concrete.Ciceksepeti;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface ICiceksepetiProductMapper
{
    Task<IDataResult<CiceksepetiCreateProductsRequest>> MapToCreateRequestAsync(Guid productId, CancellationToken ct = default);
    Task<IDataResult<CiceksepetiCreateProductsRequest>> MapToUpdateRequestAsync(Guid productId, CancellationToken ct = default);
}

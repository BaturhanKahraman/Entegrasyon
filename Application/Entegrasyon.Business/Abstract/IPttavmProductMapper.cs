using Entegrasyon.Business.Concrete.Pttavm;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IPttavmProductMapper
{
    Task<IDataResult<List<PttavmProductRequest>>> MapToUpsertRequestAsync(Guid productId, CancellationToken ct = default);
}

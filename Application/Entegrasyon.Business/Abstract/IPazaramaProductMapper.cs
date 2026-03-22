using Entegrasyon.Business.Concrete.Pazarama;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IPazaramaProductMapper
{
    Task<IDataResult<PazaramaCreateProductRequest>> MapProductAsync(Guid productId);
}

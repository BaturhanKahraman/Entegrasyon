using Entegrasyon.Business.Concrete.Pazarama;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IPazaramaBrandService
{
    Task<IDataResult<IEnumerable<PazaramaBrandDto>>> GetBrandsAsync(string? nameFilter = null, CancellationToken cancellationToken = default);
    Task<IResult> ImportBrandsAsync(CancellationToken cancellationToken = default);
}

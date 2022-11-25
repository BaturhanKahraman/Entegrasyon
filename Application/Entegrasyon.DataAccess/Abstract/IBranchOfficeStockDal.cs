using Entegrasyon.Entity.Products;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Abstract;

public interface IBranchOfficeStockDal : IEntityRepository<BranchOfficeStock>
{
    Task AddRangeAsync(IEnumerable<BranchOfficeStock> stocks);
    Task UpdateRangeAsync(List<BranchOfficeStock> stocksToDecrease);
}
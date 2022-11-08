using Entegrasyon.Entity;
using Shared;

namespace Entegrasyon.DataAccess.Abstract;

public interface IBranchOfficeDal : IEntityRepository<BranchOffice>
{
    Task<bool> CheckIfOfficesExits(IEnumerable<int> stocks);
}
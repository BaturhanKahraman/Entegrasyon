using Entegrasyon.Entity;
using Entegrasyon.Entity.Products;
using Shared;

namespace Entegrasyon.DataAccess.Abstract;

public interface IBranchOfficeDal : IEntityRepository<BranchOffice>
{
    Task<bool> CheckIfOfficesExits(int[] stocks);
}
using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Shared.Abstract.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfBranchOfficeDal:EfEntityRepository<BranchOffice,IntegrationDbContext>,IBranchOfficeDal
{
    public EfBranchOfficeDal(IntegrationDbContext ctx) : base(ctx)
    {
    }
}
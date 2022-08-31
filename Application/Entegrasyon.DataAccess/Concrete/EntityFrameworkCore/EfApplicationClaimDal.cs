using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Users;
using Shared.Abstract.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfApplicationClaimDal:EfEntityRepository<ApplicationClaim,IntegrationDbContext>, IApplicationClaimDal
{
    public EfApplicationClaimDal(IntegrationDbContext ctx) : base(ctx)
    {
    }
}
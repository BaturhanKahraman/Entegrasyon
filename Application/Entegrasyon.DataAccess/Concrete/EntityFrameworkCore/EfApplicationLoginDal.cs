using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Users;
using Shared.Abstract.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfApplicationLoginDal: EfEntityRepository<ApplicationLogin,IntegrationDbContext>, IApplicationLoginDal
{
    public EfApplicationLoginDal(IntegrationDbContext ctx) : base(ctx)
    {
    }
}
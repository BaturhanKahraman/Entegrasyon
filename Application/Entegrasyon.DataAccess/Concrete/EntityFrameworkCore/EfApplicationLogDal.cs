using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Logs;
using Shared.Abstract.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfApplicationLogDal : EfEntityRepository<ApplicationLog,IntegrationDbContext>, IApplicationLogDal
{
    
    public EfApplicationLogDal(IntegrationDbContext ctx) : base(ctx)
    {
    }
}
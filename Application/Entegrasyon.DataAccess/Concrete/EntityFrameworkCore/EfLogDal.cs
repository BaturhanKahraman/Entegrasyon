using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Logs;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfLogDal:EfEntityRepository<ApplicationLog,IntegrationDbContext>,ILogDal
{
    public EfLogDal(IntegrationDbContext ctx) : base(ctx)
    {
    }

}
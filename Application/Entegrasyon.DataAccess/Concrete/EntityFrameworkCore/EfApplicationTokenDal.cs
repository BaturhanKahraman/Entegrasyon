using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Token;
using Shared.Abstract.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfApplicationTokenDal:EfEntityRepository<ApplicationJwtToken,IntegrationDbContext>,IApplicationTokenDal
{
    public EfApplicationTokenDal(IntegrationDbContext ctx) : base(ctx)
    {
    }
}
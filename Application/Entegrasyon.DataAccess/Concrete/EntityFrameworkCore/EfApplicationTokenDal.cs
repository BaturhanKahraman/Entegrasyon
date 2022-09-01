using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Token;
using Shared.EntityFrameworkCore;
using Shared.User.Token;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfApplicationTokenDal:EfEntityRepository<RootJwtToken,IntegrationDbContext>,IApplicationTokenDal
{
    public EfApplicationTokenDal(IntegrationDbContext ctx) : base(ctx)
    {
    }
}
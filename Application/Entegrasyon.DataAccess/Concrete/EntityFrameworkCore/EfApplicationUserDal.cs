using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Users;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfApplicationUserDal: EfEntityRepository<ApplicationUser,IntegrationDbContext>, IApplicationUserDal
{
    public EfApplicationUserDal(IntegrationDbContext ctx) : base(ctx)
    {
    }
}
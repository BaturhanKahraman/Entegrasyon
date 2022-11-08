using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfCargoCompanyDal : EfEntityRepository<CargoCompany,IntegrationDbContext>, ICargoCompanyDal
{
    public EfCargoCompanyDal(IntegrationDbContext ctx) : base(ctx)
    {
    }
}
using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfApplicationCustomerDal:EfEntityRepository<ApplicationCustomer,IntegrationDbContext>,IApplicationCustomerDal
{
    public EfApplicationCustomerDal(IntegrationDbContext ctx) : base(ctx)
    {
    }
}
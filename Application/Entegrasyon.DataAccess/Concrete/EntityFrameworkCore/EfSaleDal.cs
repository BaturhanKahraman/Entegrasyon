using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Sales;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfSaleDal: EfEntityRepository<Sale,IntegrationDbContext>,ISaleDal
{
    public EfSaleDal(IntegrationDbContext ctx) : base(ctx)
    {
    }
}
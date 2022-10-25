using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfAddressDal:EfEntityRepository<Address,IntegrationDbContext>,IAddressDal
{
    public EfAddressDal(IntegrationDbContext ctx) : base(ctx)
    {
    }
}
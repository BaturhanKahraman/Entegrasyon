using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfMarketPlaceDal: EfEntityRepository<MarketPlace,IntegrationDbContext>, IMarketPlaceDal
{
    public EfMarketPlaceDal(IntegrationDbContext ctx) : base(ctx)
    {
    }
}
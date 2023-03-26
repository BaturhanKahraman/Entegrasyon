using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Products;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfMainProductDal : EfEntityRepository<Product,IntegrationDbContext>, IMainProductDal
{
    private readonly IntegrationDbContext _context;
    public EfMainProductDal(IntegrationDbContext context) : base(context)
    {
        _context = context;
    }
}
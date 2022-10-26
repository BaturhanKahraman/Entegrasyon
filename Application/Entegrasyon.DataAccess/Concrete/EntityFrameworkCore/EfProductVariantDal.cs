using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Shared.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfProductVariantDal: EfEntityRepository<ProductVariant,IntegrationDbContext>, IProductVariantDal
{
    private readonly IntegrationDbContext _ctx;
    public EfProductVariantDal(IntegrationDbContext ctx) : base(ctx)
    {
        _ctx = ctx;
    }
    public async Task<ProductVariant> GetProductVariantWithStocks(Expression<Func<ProductVariant, bool>> expr = null)
    {
        var productVariant = expr == null ? _ctx.ProductVariants : _ctx.ProductVariants.Where(expr);
        return await productVariant.Include(x => x.BranchOfficeStocks)
            .FirstOrDefaultAsync();
    }

    public async Task<List<ProductVariant>> GetProductVariantsWithStocks(Expression<Func<ProductVariant,bool>> expr=null)
    {
        var productVariant = expr == null ? _ctx.ProductVariants : _ctx.ProductVariants.Where(expr);
        return await productVariant.Include(x => x.BranchOfficeStocks)
            .ToListAsync();
    }
}
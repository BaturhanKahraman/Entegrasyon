using System.Linq.Expressions;
using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Products;
using Microsoft.EntityFrameworkCore;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Concrete.EntityFrameworkCore;

public class EfMainProductDal : EfEntityRepository<MainProduct,IntegrationDbContext>, IMainProductDal
{
    private readonly IntegrationDbContext _context;
    public EfMainProductDal(IntegrationDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<ProductDetailDto> GetProductDetail(Expression<Func<MainProduct,bool>> expr)
    {
        return await _context.MainProducts
            .Where(expr)
            .Select(x => new ProductDetailDto(x.Id,
                x.Title,
                x.Description,
                x.StockCode,
                x.Brand.Name,
                x.Category.Name,
                x.ProductVariants.
                    SelectMany(productVariant=>productVariant.BranchOfficeStocks)
                    .Sum(branchOfficeStock=>branchOfficeStock.FirstTotalStock),
                x.ProductVariants.
                    SelectMany(productVariant => productVariant.BranchOfficeStocks)
                    .Sum(branchOfficeStock => branchOfficeStock.SoldQuantity),
                x.ProductVariants.Count))
            .FirstOrDefaultAsync();
    }
}
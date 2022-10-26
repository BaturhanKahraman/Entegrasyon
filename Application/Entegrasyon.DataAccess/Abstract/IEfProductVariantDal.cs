using Entegrasyon.Entity.Products;
using Shared;
using System.Linq.Expressions;

namespace Entegrasyon.DataAccess.Abstract;

public interface IProductVariantDal : IEntityRepository<ProductVariant>
{
    Task<List<ProductVariant>> GetProductVariantsWithStocks(Expression<Func<ProductVariant, bool>> expr = null);
    Task<ProductVariant> GetProductVariantWithStocks(Expression<Func<ProductVariant, bool>> expr = null);
}
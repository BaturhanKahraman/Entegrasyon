using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Products;
using Shared;
using System.Linq.Expressions;

namespace Entegrasyon.DataAccess.Abstract;

public interface IMainProductDal : IEntityRepository<MainProduct>
{
    Task<ProductDetailDto> GetProductDetail(Expression<Func<MainProduct, bool>> expr);
}
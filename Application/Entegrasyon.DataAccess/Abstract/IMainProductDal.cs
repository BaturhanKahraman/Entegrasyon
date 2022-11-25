using Entegrasyon.Entity.Dtos.Product;
using Entegrasyon.Entity.Products;
using Shared.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Entegrasyon.DataAccess.Abstract;

public interface IMainProductDal : IEntityRepository<MainProduct>
{
    Task<ProductsDetailDto> GetProductDetail(Expression<Func<MainProduct, bool>> expr);
}
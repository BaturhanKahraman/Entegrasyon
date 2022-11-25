using Entegrasyon.Entity.Categories;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Abstract;

public interface ICategoryDal:IEntityRepository<Category>
{
    Task UpdateRangeAsync(IEnumerable<Category> categories);
}
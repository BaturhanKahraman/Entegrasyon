using Entegrasyon.Entity.Categories;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Abstract;

public interface ICategoryAttributeCategoryDal: IEntityRepository<CategoryAttributeCategory>
{
    Task UpdateRangeAsync(IEnumerable<CategoryAttributeCategory> cacList);
}
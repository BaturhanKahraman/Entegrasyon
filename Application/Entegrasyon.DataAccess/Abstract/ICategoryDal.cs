using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Abstract;

public interface ICategoryDal:IEntityRepository<Category>
{
    Task UpdateRangeAsync(IEnumerable<Category> categories);
    Task<CategoryDetailDto> ConvertToCategoryDetail(Category category);
}
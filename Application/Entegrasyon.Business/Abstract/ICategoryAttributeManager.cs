using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface ICategoryAttributeManager
{
    Task<List<CategoryAttribute>> AddIfNotExits(IEnumerable<CategoryAttribute> attrs);
    Task<bool> CheckIfCategoryHasCategoryAttribute(int categoryId);
    Task<bool> CheckIfExits(string name);
    Task<IDataResult<List<CategoryAttribute>>> GetCategoryAttributes();
    Task<IDataResult<List<CategoryAttributeDto>>> GetCategoryAttributesByCategory(int categoryId);
    Task<IResult> AddCategoryAttribute(AddCategoryAttributeDto dto);
    Task RemoveAllAttributesByCategoryId(int categoryId);
    Task RemoveAttributes(IEnumerable<CategoryAttribute> attrs);
    Task<List<CategoryAttribute>> GetCategoryAttributesByIds(IEnumerable<int> ids);
}

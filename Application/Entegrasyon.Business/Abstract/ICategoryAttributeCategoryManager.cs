using Entegrasyon.Entity.Dtos.Category;
using Shared.Results;

namespace Entegrasyon.Business.Abstract;

public interface ICategoryAttributeCategoryManager
{
    Task<IResult> AddCategoryAttributeForCategory(int catId, IEnumerable<AddCategoryAttributeDto> dto);
}

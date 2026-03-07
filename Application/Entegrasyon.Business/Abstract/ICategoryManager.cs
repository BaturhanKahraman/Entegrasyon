using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Dtos.Category.AddStep;
using Shared.DTO;
using Shared.Entity;
using Shared.Results;

namespace Entegrasyon.Business.Abstract;

public interface ICategoryService
{
    Task<IResult> AddCategoryStepOne(AddCategoryDtoStepOne dto);
    Task<IDataResult<CategoryDetailDto>> AddCategory(AddCategoryDto dto);
    Task<IResult> UpdateCategory(EditCategoryDto dto);
    Task<IResult> DeleteCategory(int categoryId);
    Task<IResult> SoftDelete(int categoryId);
    Task<IDataResult<List<CategoryDetailDto>>> GetCategoryDetailList();
    Task<IDataResult<Pageable<CategoryDetailDto>>> GetCategoryDetailPageable(int pageIndex = 1, int itemCount = 50, string categoryName = null);
    Task<IResult> AddFavorite(int categoryId);
    Task<IResult> AddFavorites(int[] categoryIds);
    Task<IDataResult<List<CategoryDetailDto>>> GetFavoriteCategories();
    Task<IDataResult<List<CategoryDetailDto>>> GetSubCategories();
    Task<IDataResult<List<CategoryDetailDto>>> GetSuperCategories();
    Task<IDataResult<Category>> GetCategoryEditDetail(int id);
    Task<bool> Exits(int id);
    Task<string> GetCategoryNameById(int categoryId);
    Task<Category> GetCategoryById(int? categoryId);
    Task<Category> GetCategoryWithAttrById(int? categoryId);
    Task UpdatePlainCategory(Category category);
    Task<bool> IsSuper(int? categoryId);
    Task<List<Category>> GetAllCategoriesWithHierarchyAsync();
    Task<List<Category>> GetAllCategoriesWithoutAttributesAsync();
}

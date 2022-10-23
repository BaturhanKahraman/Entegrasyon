using System.Linq.Expressions;
using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Logs;
using Microsoft.EntityFrameworkCore;
using Shared.Entity;
using Shared.EntityFrameworkCore;
using Shared.Extensions;
using Shared.Results;

namespace Entegrasyon.Business.Concrete
{
    public class CategoryManager
    {
        private readonly ICategoryDal _categoryDal;
        private readonly ApplicationLogManager _applicationLogManager;

        public CategoryManager(ICategoryDal categoryDal,ApplicationLogManager applicationLogManager)
        {
            _categoryDal = categoryDal;
            _applicationLogManager = applicationLogManager;
        }

        public async Task<IResult> AddCategory(AddCategoryDto dto)
        {
            await _applicationLogManager.AddLog("Kategori ekleniyor.",LogType.Category,LogAction.Add,dto);
            var category = new Category
            {
                Name = dto.Name,
                SuperCategoryId = dto.SuperCategoryId,
            };
            if (dto.CategoryAttributes != null)
                category.CategoryAttributes = dto.CategoryAttributes.ToList();

            await _categoryDal.AddAsync(category);
            await _applicationLogManager.AddLog("Kategori eklendi.",LogType.Category,LogAction.Add);
            return new SuccessResult();
        }

        public async Task UpdateCategory(UpdateCategoryDto dto)
        {
            await _applicationLogManager.AddLog("Kategori güncelleniyor.",LogType.Category,LogAction.Update,dto);
            var category = await _categoryDal.GetAsync(x => x.Id == dto.Id,true);
            category.Name = dto.Name ?? category.Name;
            category.SuperCategoryId = dto.SuperCategoryId ?? category.SuperCategoryId;
            category.CategoryAttributes = dto.CategoryAttributes ?? category.CategoryAttributes;
            await _categoryDal.UpdateAsync(category);
            await _applicationLogManager.AddLog("Kategori güncellendi.",LogType.Category,LogAction.Update,dto);
        }

        public async Task DeleteCategory(int categoryId)
        {
            await _applicationLogManager.AddLog("Kategori siliniyor.",LogType.Category,LogAction.Delete,new { categoryId });
            var category = await _categoryDal.GetAsync(x => x.Id == categoryId);
            await _categoryDal.DeleteAsync(category);
            await _applicationLogManager.AddLog("Kategori silindi.",LogType.Category,LogAction.Delete,new { categoryId });
        }

        public async Task<IDataResult<List<CategoryDetailDto>>> GetCategoryDetailList()
        {
            var categoriesDto =
                await _categoryDal.Table.Select(x => new CategoryDetailDto(x.Id,x.Products.Count,x.Name,x.SubCategories.Count)).ToListAsync();
            return new SuccessDataResult<List<CategoryDetailDto>>(categoriesDto);
        }
        public async Task<IDataResult<Pageable<CategoryDetailDto>>> GetCategoryDetailPageable(int pageIndex = 1,int itemCount = 50,string categoryName=null)
        {
            var orderBy = new List<(string,string)>
            {
                new ("Id", "desc")
            };
            Expression<Func<Category,bool>> filter = !string.IsNullOrEmpty(categoryName)
                ? x => EF.Functions.ILike(x.Name, $"%{categoryName}%")
                : null;
            
            var categoriesDto = await _categoryDal.GetPaginatedTransformedEntities(pageIndex,itemCount,
                x => new CategoryDetailDto(x.Id,x.Products.Count,x.Name,x.SubCategories.Count),orderBy,filter);
            return new SuccessDataResult<Pageable<CategoryDetailDto>>(categoriesDto);
        }
        public async Task GetCategoryDetailById()
        {
            throw new NotImplementedException();
        }

        public async Task AddMultipleCategory(IEnumerable<Category> categories)
        {
            throw new NotImplementedException();
        }
    }
}

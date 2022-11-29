using AutoMapper;
using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Logs;
using Microsoft.EntityFrameworkCore;
using Shared.Entity;
using Shared.Results;
using System.Linq.Expressions;

namespace Entegrasyon.Business.Concrete
{
    public class CategoryManager
    {
        private readonly ICategoryDal _categoryDal;
        private readonly ApplicationLogManager _applicationLogManager;
        private readonly IMapper _mapper;

        public CategoryManager(ICategoryDal categoryDal, ApplicationLogManager applicationLogManager, IMapper mapper)
        {
            _categoryDal = categoryDal;
            _applicationLogManager = applicationLogManager;
            _mapper = mapper;
        }

        public async Task<IResult> AddCategory(AddCategoryDto dto)
        {
            await _applicationLogManager.AddLog("Kategori ekleniyor.",LogType.Category,LogAction.Add,dto);
            var category = _mapper.Map<Category>(dto);
            //TODO
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
            var orderTuples = new List<(string, string)>
            {
                new ("IsFavorite", "desc"),
                new ("Id", "desc")
            };
            var categoriesDto = await _categoryDal.GetTransformedEntitiesAsync(x => new CategoryDetailDto(
                x.Id,
                x.Products.Sum(p => p.ProductVariants
                    .SelectMany(pv => pv.BranchOfficeStocks)
                    .Sum(bo => bo.CurrentStock)),x.Name,x.SubCategories.Count,x.IsFavorite),orderTuples: orderTuples);

            return new SuccessDataResult<List<CategoryDetailDto>>(categoriesDto);
        }
        public async Task<IDataResult<Pageable<CategoryDetailDto>>> GetCategoryDetailPageable(int pageIndex = 1,int itemCount = 50,string categoryName = null)
        {
            var orderBy = new List<(string, string)>
            {
                new ("Id", "desc")
            };
            Expression<Func<Category,bool>> filter = !string.IsNullOrEmpty(categoryName)
                ? x => EF.Functions.ILike(x.Name,$"%{categoryName}%")
                : null;

            var categoriesDto = await _categoryDal.GetPaginatedTransformedEntities(pageIndex,itemCount,
                x => new CategoryDetailDto(x.Id,x.Products.Count,x.Name,x.SubCategories.Count,x.IsFavorite),orderBy,filter);
            return new SuccessDataResult<Pageable<CategoryDetailDto>>(categoriesDto);
        }

        public async Task<IResult> AddFavorite(int categoryId)
        {
            var category = await _categoryDal.GetAsync(x => x.Id == categoryId);
            if(category == null)
                return new ErrorResult("Böyle bir kategori bulunamadı.");
            category.IsFavorite = true;
            await _categoryDal.UpdateAsync(category);
            return new SuccessResult("Kategori başarı ile favorilere eklendi.");
        }
        public async Task<IResult> AddFavorites(int[] categoryIds)
        {
            var categories = await _categoryDal.GetAllAsync(x => categoryIds.Contains(x.Id));
            if(categories == null || !categories.Any())
                return new ErrorResult("Bulunamayan kategori var.");
            categories.ForEach(x => x.IsFavorite = true);
            await _categoryDal.UpdateRangeAsync(categories);
            return new SuccessResult("Kategoriler başarı ile favorilere eklendi.");
        }

        public async Task<IDataResult<List<CategoryDetailDto>>> GetFavoriteCategories()
        {
            var orderTuples = new List<(string, string)>
            {
                new("Id", "desc")
            };
            var result = await _categoryDal.GetTransformedEntitiesAsync(x =>
                    new CategoryDetailDto(x.Id,x.Products.Count,x.Name,x.SubCategories.Count,x.IsFavorite),orderTuples,x => x.IsFavorite)
                ;
            return new SuccessDataResult<List<CategoryDetailDto>>(result);
        }
        public async Task<IDataResult<List<CategoryDetailDto>>> GetSubCategories()
        {
            var orderTuples = new List<(string, string)>
            {
                new ("IsFavorite","desc"),
                new("Name","asc"),
                new("Id", "desc"),
            };
            var result = await _categoryDal.GetTransformedEntitiesAsync(x =>
                    new CategoryDetailDto(x.Id,x.Products.Count,x.Name,x.SubCategories.Count,x.IsFavorite),orderTuples,
                x => x.SubCategories.Count==0 && x.CategoryAttributes.Count>=1);
            return new SuccessDataResult<List<CategoryDetailDto>>(result);
        }
    }
}

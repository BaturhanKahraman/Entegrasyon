using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Logs;
using Microsoft.EntityFrameworkCore;
using Shared.Entity;
using Shared.Results;
using System.Linq.Expressions;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Dtos.Category.AddStep;
using Entegrasyon.Business.Abstract;
using MapsterMapper;

namespace Entegrasyon.Business.Concrete
{
    public class CategoryManager(ICategoryDal categoryDal,IApplicationLogManager applicationLogManager,IMapper mapper,IFluentValidator fluentValidator,IProductService productService) : ICategoryService
    {
        private readonly ICategoryDal _categoryDal = categoryDal;
        private readonly IApplicationLogManager _applicationLogManager = applicationLogManager;
        private readonly IMapper _mapper = mapper;
        private readonly IFluentValidator _fluentValidator = fluentValidator;
        private readonly IProductService _productService = productService;

        public async Task<IResult> AddCategoryStepOne(AddCategoryDtoStepOne dto)
        {
            await _fluentValidator.ValidateAndThrowAsync(dto);
            //map
            var category = _mapper.Map<Category>(dto);
            if (await _categoryDal.Exists(x => string.Equals(dto.Name, x.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return new ErrorResult(Messages.SameNameCategoryExits);
            }
            await _categoryDal.AddAsync(category);
            return new SuccessResult(Messages.CategoryAdded);
        }

        public async Task<IDataResult<CategoryDetailDto>> AddCategory(AddCategoryDto dto)
        {
            await _applicationLogManager.AddLog("Kategori ekleniyor.", LogType.Category, LogAction.Add, dto);
            var category = _mapper.Map<Category>(dto);
            await _categoryDal.AddAsync(category);
            await _applicationLogManager.AddLog(Messages.CategoryAdded, LogType.Category, LogAction.Add);
            CategoryDetailDto detail = await _categoryDal.ConvertToCategoryDetail(category);
            return new SuccessDataResult<CategoryDetailDto>(detail);
        }

        public async Task<IResult> UpdateCategory(EditCategoryDto dto)
        {
            await _fluentValidator.ValidateAndThrowAsync(dto);
            var dbCategory = await _categoryDal.GetAsync(x => x.Id == dto.Id, true);
            if (dbCategory == null)
                return new ErrorDataResult<CategoryDetailDto>(null, "Kategori bulunamadı.");
            dbCategory.SuperCategoryId = dto.SuperCategoryId;
            dbCategory.Name = dto.Name;
            dbCategory.IsFavorite = dto.IsFavorite;
            dbCategory.IsImported = dto.IsImported;
            await _categoryDal.UpdateAsync(dbCategory);
            return new SuccessResult(Messages.CategoryUpdated);
        }

        public async Task<IResult> DeleteCategory(int categoryId)
        {
            await _applicationLogManager.AddLog("Kategori siliniyor.", LogType.Category, LogAction.Delete, new { categoryId });
            var category = await _categoryDal.GetAsync(x => x.Id == categoryId);
            if (category == null)
                return new ErrorResult(Messages.CategoryNotFound);
            await _categoryDal.DeleteAsync(category);
            await _applicationLogManager.AddLog("Kategori silindi.", LogType.Category, LogAction.Delete, new { categoryId });
            return new SuccessResult(Messages.CategoryDeleted);
        }
        //eğer altında ürün varsa silinmemeli
        public async Task<IResult> SoftDelete(int categoryId)
        {
            await _applicationLogManager.AddLog("Kategori siliniyor.", LogType.Category, LogAction.Delete, new { categoryId });
            var category = await _categoryDal.GetAsync(x => x.Id == categoryId);
            if (category == null)
                return new ErrorResult(Messages.CategoryNotFound);
            int productCount = await _productService.GetProductCountByCategoryId(categoryId);
            if (productCount > 0)
                return new ErrorResult(Messages.CategoryHasProducts);
            await _categoryDal.SoftDeleteAsync(category);
            await _applicationLogManager.AddLog("Kategori silindi.", LogType.Category, LogAction.Delete, new { categoryId });
            return new SuccessResult(Messages.CategoryDeleted);
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
                    .Sum(bo => bo.CurrentStock)), x.Name, x.SubCategories.Count(), x.IsFavorite, x.CategoryAttributes.Count(), x.SuperCategory.Name), orderTuples: orderTuples);

            return new SuccessDataResult<List<CategoryDetailDto>>(categoriesDto);
        }
        public async Task<IDataResult<Pageable<CategoryDetailDto>>> GetCategoryDetailPageable(int pageIndex = 1, int itemCount = 50, string categoryName = null)
        {
            var orderBy = new List<(string, string)>
            {
                new ("Id", "desc")
            };
            Expression<Func<Category, bool>> filter = !string.IsNullOrEmpty(categoryName)
                ? x => EF.Functions.ILike(x.Name, $"%{categoryName}%")
                : null;

            var categoriesDto = await _categoryDal.GetPaginatedTransformedEntities(pageIndex, itemCount,
                x => new CategoryDetailDto(x.Id, x.Products.Count(), x.Name, x.SubCategories.Count(), x.IsFavorite, x.CategoryAttributes.Count(), x.SuperCategory.Name), orderBy, filter);
            return new SuccessDataResult<Pageable<CategoryDetailDto>>(categoriesDto);
        }

        public async Task<IResult> AddFavorite(int categoryId)
        {
            var category = await _categoryDal.GetAsync(x => x.Id == categoryId);
            if (category == null)
                return new ErrorResult("Böyle bir kategori bulunamadı.");
            category.IsFavorite = true;
            await _categoryDal.UpdateAsync(category);
            return new SuccessResult("Kategori başarı ile favorilere eklendi.");
        }
        public async Task<IResult> AddFavorites(int[] categoryIds)
        {
            var categories = await _categoryDal.GetAllAsync(x => categoryIds.Contains(x.Id));
            if (categories == null || !categories.Any())
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
                    new CategoryDetailDto(x.Id, x.Products.Count(), x.Name, x.SubCategories.Count(), x.IsFavorite, x.CategoryAttributes.Count(), x.SuperCategory.Name), orderTuples, x => x.IsFavorite)
                ;
            return new SuccessDataResult<List<CategoryDetailDto>>(result);
        }
        public async Task<IDataResult<List<CategoryDetailDto>>> GetSubCategories()
        {
            var orderTuples = new List<(string, string)>
            {
                new ("IsFavorite","desc"),
                new("Name","asc"),
            };
            var result = await _categoryDal.GetTransformedEntitiesAsync(x =>
                    new CategoryDetailDto(x.Id,
                    x.Products.Count(),
                    x.Name,
                    x.SubCategories.Count(),
                    x.IsFavorite,
                    x.CategoryAttributes.Count(),
                    x.SuperCategory.Name), orderTuples,
                x => !x.SubCategories.Any());
            return new SuccessDataResult<List<CategoryDetailDto>>(result);
        }

        public async Task<IDataResult<List<CategoryDetailDto>>> GetSuperCategories()
        {
            var orderTuples = new List<(string, string)>
            {
                new ("IsFavorite","desc"),
                new("Name","asc"),
            };
            var result = await _categoryDal.GetTransformedEntitiesAsync(x =>
                    new CategoryDetailDto(x.Id,
                    x.Products.Count(),
                    x.Name,
                    x.SubCategories.Count(),
                    x.IsFavorite,
                    x.CategoryAttributes.Count(),
                    x.SuperCategory.Name), orderTuples,
                x => !x.CategoryAttributes.Any());
            return new SuccessDataResult<List<CategoryDetailDto>>(result);
        }

        public async Task<IDataResult<Category>> GetCategoryEditDetail(int id)
        {
            var result = await _categoryDal.GetAsync(x => x.Id == id);
            return new SuccessDataResult<Category>(result);
        }


        public async Task<bool> Exits(int id)
        {
            if (id <= 0)
                return false;
            return await _categoryDal.Exists(x => x.Id == id);
        }

        public Task<string> GetCategoryNameById(int categoryId)
        {
            return _categoryDal.Table
                .AsNoTracking()
                .Where(x => x.Id == categoryId)
                .Select(x => x.Name)
                .FirstOrDefaultAsync();
        }

        public Task<Category> GetCategoryById(int? categoryId) =>
            _categoryDal.GetAsync(x => x.Id == categoryId);

        public Task<Category> GetCategoryWithAttrById(int? categoryId) =>
            _categoryDal.Table.Include(x => x.CategoryAttributes).FirstOrDefaultAsync(x => x.Id == categoryId);

        public Task UpdatePlainCategory(Category category) => _categoryDal.UpdateAsync(category);

        public Task<bool> IsSuper(int? categoryId) => _categoryDal
                .Exists(c => c.Id == categoryId && c.SubCategories.Any());

        /// <summary>
        /// Tüm kategorileri hiyerarşi bilgisiyle birlikte getirir
        /// </summary>
        public async Task<List<Category>> GetAllCategoriesWithHierarchyAsync()
        {
            return await _categoryDal.Table
                .AsNoTracking()
                .Include(c => c.CategoryAttributes)
                    .ThenInclude(ca => ca.CategoryAttribute)
                .Include(c => c.MarketplaceLinks)
                    .ThenInclude(ml => ml.MarketPlace)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }
        /// <summary>
        /// Kategorileri sadece Id ve Name bilgileriyle getirir (CategoryAttributes olmadan)
        /// </summary>
        public async Task<List<Category>> GetAllCategoriesWithoutAttributesAsync()
        {
            return await _categoryDal.Table
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();
        }
        /// <summary>
        /// Belirli bir kategorinin marketplace eşleşmelerini getirir
        /// </summary>
        public async Task<List<CategoryMarketplace>> GetCategoryMarketplaceLinksAsync(int categoryId)
        {
            return await _categoryDal.Table
                .AsNoTracking()
                .Where(c => c.Id == categoryId)
                .SelectMany(c => c.MarketplaceLinks)
                .Include(cm => cm.MarketPlace)
                .ToListAsync();
        }

    }
}

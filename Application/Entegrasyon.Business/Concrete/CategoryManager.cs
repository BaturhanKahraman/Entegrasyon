using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Logs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Entegrasyon.Entity.Results;
using System.Linq.Expressions;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Dtos.Category.AddStep;
using Entegrasyon.Business.Abstract;
using MapsterMapper;
using System.Linq.Dynamic.Core;

namespace Entegrasyon.Business.Concrete
{
    public class CategoryManager(IntegrationDbContext dbContext, IApplicationLogManager applicationLogManager, IMapper mapper, IFluentValidator fluentValidator, IProductService productService, IMemoryCache cache) : ICategoryService
    {
        private const string CategoryListCacheKey = "categories:list";
        public async Task<IResult> AddCategoryStepOne(AddCategoryDtoStepOne dto)
        {
            await fluentValidator.ValidateAndThrowAsync(dto);
            var category = mapper.Map<Category>(dto);
            if (await dbContext.Categories.AnyAsync(x => x.Name.ToLower() == dto.Name.ToLower()))
                return new ErrorResult(Messages.SameNameCategoryExits);
            dbContext.Categories.Add(category);
            await dbContext.SaveChangesAsync();
            cache.Remove(CategoryListCacheKey);
            return new SuccessResult(Messages.CategoryAdded);
        }

        public async Task<IDataResult<CategoryDetailDto>> AddCategory(AddCategoryDto dto)
        {
            await applicationLogManager.AddLog("Kategori ekleniyor.", LogType.Category, LogAction.Add, dto);
            var category = mapper.Map<Category>(dto);
            dbContext.Categories.Add(category);
            await dbContext.SaveChangesAsync();
            cache.Remove(CategoryListCacheKey);
            await applicationLogManager.AddLog(Messages.CategoryAdded, LogType.Category, LogAction.Add);
            var detail = await dbContext.Categories
                .Where(x => x.Id == category.Id)
                .Select(x => new CategoryDetailDto(x.Id, x.Products.Count(), x.Name, x.SubCategories.Count(), x.IsFavorite, x.CategoryAttributes.Count(), x.SuperCategory.Name))
                .FirstOrDefaultAsync();
            return new SuccessDataResult<CategoryDetailDto>(detail);
        }

        public async Task<IResult> UpdateCategory(EditCategoryDto dto)
        {
            await fluentValidator.ValidateAndThrowAsync(dto);
            var dbCategory = await dbContext.Categories.AsTracking().FirstOrDefaultAsync(x => x.Id == dto.Id);
            if (dbCategory == null)
                return new ErrorDataResult<CategoryDetailDto>(null, "Kategori bulunamadı.");
            dbCategory.SuperCategoryId = dto.SuperCategoryId;
            dbCategory.Name = dto.Name;
            dbCategory.IsFavorite = dto.IsFavorite;
            dbCategory.IsImported = dto.IsImported;
            await dbContext.SaveChangesAsync();
            cache.Remove(CategoryListCacheKey);
            return new SuccessResult(Messages.CategoryUpdated);
        }

        public async Task<IResult> DeleteCategory(int categoryId)
        {
            await applicationLogManager.AddLog("Kategori siliniyor.", LogType.Category, LogAction.Delete, new { categoryId });
            var category = await dbContext.Categories.FirstOrDefaultAsync(x => x.Id == categoryId);
            if (category == null)
                return new ErrorResult(Messages.CategoryNotFound);
            dbContext.Categories.Remove(category);
            await dbContext.SaveChangesAsync();
            cache.Remove(CategoryListCacheKey);
            await applicationLogManager.AddLog("Kategori silindi.", LogType.Category, LogAction.Delete, new { categoryId });
            return new SuccessResult(Messages.CategoryDeleted);
        }

        public async Task<IResult> SoftDelete(int categoryId)
        {
            await applicationLogManager.AddLog("Kategori siliniyor.", LogType.Category, LogAction.Delete, new { categoryId });
            var category = await dbContext.Categories.AsTracking().FirstOrDefaultAsync(x => x.Id == categoryId);
            if (category == null)
                return new ErrorResult(Messages.CategoryNotFound);
            int productCount = await productService.GetProductCountByCategoryId(categoryId);
            if (productCount > 0)
                return new ErrorResult(Messages.CategoryHasProducts);
            category.IsDeleted = true;
            category.DeletedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync();
            cache.Remove(CategoryListCacheKey);
            await applicationLogManager.AddLog("Kategori silindi.", LogType.Category, LogAction.Delete, new { categoryId });
            return new SuccessResult(Messages.CategoryDeleted);
        }

        public async Task<IDataResult<List<CategoryDetailDto>>> GetCategoryDetailList()
        {
            var result = await dbContext.Categories
                .OrderByDescending(x => x.IsFavorite)
                .ThenByDescending(x => x.Id)
                .Select(x => new CategoryDetailDto(
                    x.Id,
                    x.Products.Sum(p => p.ProductVariants.SelectMany(pv => pv.BranchOfficeStocks).Sum(bo => bo.CurrentStock)),
                    x.Name, x.SubCategories.Count(), x.IsFavorite, x.CategoryAttributes.Count(), x.SuperCategory.Name))
                .ToListAsync();
            return new SuccessDataResult<List<CategoryDetailDto>>(result);
        }

        public async Task<IDataResult<Pageable<CategoryDetailDto>>> GetCategoryDetailPageable(int pageIndex = 1, int itemCount = 50, string categoryName = null)
        {
            var query = dbContext.Categories.AsQueryable();
            if (!string.IsNullOrEmpty(categoryName))
                query = query.Where(x => EF.Functions.ILike(x.Name, $"%{categoryName}%"));

            int total = await query.CountAsync();
            var items = await query
                .OrderByDescending(x => x.Id)
                .Skip((pageIndex - 1) * itemCount)
                .Take(itemCount)
                .Select(x => new CategoryDetailDto(x.Id, x.Products.Count(), x.Name, x.SubCategories.Count(), x.IsFavorite, x.CategoryAttributes.Count(), x.SuperCategory.Name))
                .ToListAsync();

            return new SuccessDataResult<Pageable<CategoryDetailDto>>(new Pageable<CategoryDetailDto>(items, pageIndex, itemCount, total));
        }

        public async Task<IResult> AddFavorite(int categoryId)
        {
            var category = await dbContext.Categories.AsTracking().FirstOrDefaultAsync(x => x.Id == categoryId);
            if (category == null)
                return new ErrorResult("Böyle bir kategori bulunamadı.");
            category.IsFavorite = true;
            await dbContext.SaveChangesAsync();
            return new SuccessResult("Kategori başarı ile favorilere eklendi.");
        }

        public async Task<IResult> AddFavorites(int[] categoryIds)
        {
            var categories = await dbContext.Categories.AsTracking().Where(x => categoryIds.Contains(x.Id)).ToListAsync();
            if (categories == null || !categories.Any())
                return new ErrorResult("Bulunamayan kategori var.");
            categories.ForEach(x => x.IsFavorite = true);
            await dbContext.SaveChangesAsync();
            return new SuccessResult("Kategoriler başarı ile favorilere eklendi.");
        }

        public async Task<IDataResult<List<CategoryDetailDto>>> GetFavoriteCategories()
        {
            var result = await dbContext.Categories
                .Where(x => x.IsFavorite)
                .OrderByDescending(x => x.Id)
                .Select(x => new CategoryDetailDto(x.Id, x.Products.Count(), x.Name, x.SubCategories.Count(), x.IsFavorite, x.CategoryAttributes.Count(), x.SuperCategory.Name))
                .ToListAsync();
            return new SuccessDataResult<List<CategoryDetailDto>>(result);
        }

        public async Task<IDataResult<List<CategoryDetailDto>>> GetSubCategories()
        {
            var result = await dbContext.Categories
                .Where(x => !x.SubCategories.Any())
                .OrderByDescending(x => x.IsFavorite).ThenBy(x => x.Name)
                .Select(x => new CategoryDetailDto(x.Id, x.Products.Count(), x.Name, x.SubCategories.Count(), x.IsFavorite, x.CategoryAttributes.Count(), x.SuperCategory.Name))
                .ToListAsync();
            return new SuccessDataResult<List<CategoryDetailDto>>(result);
        }

        public async Task<IDataResult<List<CategoryDetailDto>>> GetSuperCategories()
        {
            var result = await dbContext.Categories
                .Where(x => !x.CategoryAttributes.Any())
                .OrderByDescending(x => x.IsFavorite).ThenBy(x => x.Name)
                .Select(x => new CategoryDetailDto(x.Id, x.Products.Count(), x.Name, x.SubCategories.Count(), x.IsFavorite, x.CategoryAttributes.Count(), x.SuperCategory.Name))
                .ToListAsync();
            return new SuccessDataResult<List<CategoryDetailDto>>(result);
        }

        public async Task<IDataResult<Category>> GetCategoryEditDetail(int id)
        {
            var result = await dbContext.Categories.FirstOrDefaultAsync(x => x.Id == id);
            return new SuccessDataResult<Category>(result);
        }

        public async Task<bool> Exits(int id)
        {
            if (id <= 0) return false;
            return await dbContext.Categories.AnyAsync(x => x.Id == id);
        }

        public Task<string> GetCategoryNameById(int categoryId) =>
            dbContext.Categories.AsNoTracking().Where(x => x.Id == categoryId).Select(x => x.Name).FirstOrDefaultAsync();

        public Task<Category> GetCategoryById(int? categoryId) =>
            dbContext.Categories.FirstOrDefaultAsync(x => x.Id == categoryId);

        public Task<Category> GetCategoryWithAttrById(int? categoryId) =>
            dbContext.Categories.Include(x => x.CategoryAttributes).FirstOrDefaultAsync(x => x.Id == categoryId);

        public async Task UpdatePlainCategory(Category category)
        {
            dbContext.Categories.Update(category);
            await dbContext.SaveChangesAsync();
        }

        public Task<bool> IsSuper(int? categoryId) =>
            dbContext.Categories.AnyAsync(c => c.Id == categoryId && c.SubCategories.Any());

        public async Task<List<Category>> GetAllCategoriesWithHierarchyAsync()
        {
            return await dbContext.Categories
                .AsNoTracking()
                .Include(c => c.CategoryAttributes).ThenInclude(ca => ca.CategoryAttribute)
                .Include(c => c.MarketplaceLinks).ThenInclude(ml => ml.MarketPlace)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<List<Category>> GetAllCategoriesWithoutAttributesAsync()
        {
            if (cache.TryGetValue(CategoryListCacheKey, out List<Category> cached))
                return cached;

            var result = await dbContext.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
            cache.Set(CategoryListCacheKey, result, TimeSpan.FromMinutes(30));
            return result;
        }

        public async Task<List<CategoryMarketplace>> GetCategoryMarketplaceLinksAsync(int categoryId)
        {
            return await dbContext.Categories
                .AsNoTracking()
                .Where(c => c.Id == categoryId)
                .SelectMany(c => c.MarketplaceLinks)
                .Include(cm => cm.MarketPlace)
                .ToListAsync();
        }
    }
}

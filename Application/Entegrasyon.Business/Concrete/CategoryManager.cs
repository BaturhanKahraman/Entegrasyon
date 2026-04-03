using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Category;
using Entegrasyon.Entity.Dtos.Storefront;
using Entegrasyon.Entity.Logs;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.Entity.Results;
using Entegrasyon.Business.Tenants;
using System.Linq.Expressions;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.Entity.Dtos.Category.AddStep;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Mappers;
using System.Linq.Dynamic.Core;

namespace Entegrasyon.Business.Concrete
{
    public class CategoryManager(IDbContextFactory<IntegrationDbContext> contextFactory, IApplicationLogManager applicationLogManager, CategoryMapper mapper, IFluentValidator fluentValidator, IProductService productService, TenantMemoryCache cache) : ICategoryService
    {
        private const string CategoryListCacheKey = "categories:list";
        public async Task<IResult> AddCategoryStepOne(AddCategoryDtoStepOne dto)
        {
            using var dbContext = contextFactory.CreateDbContext();
            await fluentValidator.ValidateAndThrowAsync(dto);
            var category = mapper.MapToEntity(dto);
            if (await dbContext.Categories.AnyAsync(x => x.Name.ToLower() == dto.Name.ToLower()))
                return new ErrorResult(Messages.SameNameCategoryExits);
            dbContext.Categories.Add(category);
            await dbContext.SaveChangesAsync();
            cache.Remove(CategoryListCacheKey);
            return new SuccessResult(Messages.CategoryAdded);
        }

        public async Task<IDataResult<CategoryDetailDto>> AddCategory(AddCategoryDto dto)
        {
            using var dbContext = contextFactory.CreateDbContext();
            await applicationLogManager.AddLog("Kategori ekleniyor.", LogType.Category, LogAction.Add, dto);

            if (dto.SuperCategoryId is > 0)
            {
                bool parentHasAttrs = await dbContext.CategoryAttributeCategories
                    .AnyAsync(x => x.CategoryId == dto.SuperCategoryId.Value);
                if (parentHasAttrs)
                    return new ErrorDataResult<CategoryDetailDto>(null!, "Seçilen üst kategori özellik içerdiğinden alt kategori eklenemez.");

                bool parentHasSync = await dbContext.CategoryMarketplaces
                    .AnyAsync(x => x.CategoryId == dto.SuperCategoryId.Value && x.IsActive);
                if (parentHasSync)
                    return new ErrorDataResult<CategoryDetailDto>(null!, "Seçilen üst kategori pazar yeri eşleştirmesi içerdiğinden alt kategori eklenemez.");
            }

            var category = mapper.MapToEntity(dto);
            dbContext.Categories.Add(category);
            await dbContext.SaveChangesAsync();
            cache.Remove(CategoryListCacheKey);
            await applicationLogManager.AddLog(Messages.CategoryAdded, LogType.Category, LogAction.Add);
            var detail = await dbContext.Categories
                .Where(x => x.Id == category.Id)
                .Select(x => new CategoryDetailDto(x.Id, x.Products.Count(), x.Name, x.SubCategories.Count(), x.IsFavorite, x.CategoryAttributes.Count(), x.SuperCategory!.Name))
                .FirstOrDefaultAsync();
            return new SuccessDataResult<CategoryDetailDto>(detail!);
        }

        public async Task<IResult> UpdateCategory(EditCategoryDto dto)
        {
            using var dbContext = contextFactory.CreateDbContext();
            await fluentValidator.ValidateAndThrowAsync(dto);
            var dbCategory = await dbContext.Categories.AsTracking().FirstOrDefaultAsync(x => x.Id == dto.Id);
            if (dbCategory == null)
                return new ErrorDataResult<CategoryDetailDto>(null!, "Kategori bulunamadı.");

            if (dto.SuperCategoryId is > 0)
            {
                bool parentHasAttrs = await dbContext.CategoryAttributeCategories
                    .AnyAsync(x => x.CategoryId == dto.SuperCategoryId.Value);
                if (parentHasAttrs)
                    return new ErrorResult("Seçilen üst kategori özellik içerdiğinden bu işlem yapılamaz.");

                bool parentHasSync = await dbContext.CategoryMarketplaces
                    .AnyAsync(x => x.CategoryId == dto.SuperCategoryId.Value && x.IsActive);
                if (parentHasSync)
                    return new ErrorResult("Seçilen üst kategori pazar yeri eşleştirmesi içerdiğinden bu işlem yapılamaz.");
            }

            dbCategory.SuperCategoryId = dto.SuperCategoryId;
            dbCategory.Name = dto.Name;
            dbCategory.IsFavorite = dto.IsFavorite;
            dbCategory.IsImported = dto.IsImported;
            dbCategory.DefaultVatRate = dto.DefaultVatRate;
            await dbContext.SaveChangesAsync();
            cache.Remove(CategoryListCacheKey);
            return new SuccessResult(Messages.CategoryUpdated);
        }

        public async Task<IResult> DeleteCategory(int categoryId)
        {
            using var dbContext = contextFactory.CreateDbContext();
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
            using var dbContext = contextFactory.CreateDbContext();
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
            using var dbContext = contextFactory.CreateDbContext();
            var result = await dbContext.CategorySummaries
                .OrderByDescending(x => x.IsFavorite)
                .ThenByDescending(x => x.CategoryId)
                .Select(x => new CategoryDetailDto(
                    x.CategoryId, x.TotalStock, x.Name,
                    x.SubCategoryCount, x.IsFavorite, x.AttributeCount, x.SuperCategoryName!))
                .ToListAsync();
            return new SuccessDataResult<List<CategoryDetailDto>>(result);
        }

        public async Task<IDataResult<Pageable<CategoryDetailDto>>> GetCategoryDetailPageable(int pageIndex = 1, int itemCount = 50, string? categoryName = null)
        {
            using var dbContext = contextFactory.CreateDbContext();
            var query = dbContext.CategorySummaries.AsQueryable();
            if (!string.IsNullOrEmpty(categoryName))
                query = query.Where(x => EF.Functions.ILike(x.Name, $"%{categoryName}%"));

            int total = await query.CountAsync();
            var items = await query
                .OrderByDescending(x => x.CategoryId)
                .Skip((pageIndex - 1) * itemCount)
                .Take(itemCount)
                .Select(x => new CategoryDetailDto(
                    x.CategoryId, x.ProductCount, x.Name,
                    x.SubCategoryCount, x.IsFavorite, x.AttributeCount, x.SuperCategoryName!))
                .ToListAsync();

            return new SuccessDataResult<Pageable<CategoryDetailDto>>(new Pageable<CategoryDetailDto>(items, pageIndex, itemCount, total));
        }

        public async Task<IResult> AddFavorite(int categoryId)
        {
            using var dbContext = contextFactory.CreateDbContext();
            var category = await dbContext.Categories.AsTracking().FirstOrDefaultAsync(x => x.Id == categoryId);
            if (category == null)
                return new ErrorResult("Böyle bir kategori bulunamadı.");
            category.IsFavorite = true;
            await dbContext.SaveChangesAsync();
            return new SuccessResult("Kategori başarı ile favorilere eklendi.");
        }

        public async Task<IResult> AddFavorites(int[] categoryIds)
        {
            using var dbContext = contextFactory.CreateDbContext();
            var categories = await dbContext.Categories.AsTracking().Where(x => categoryIds.Contains(x.Id)).ToListAsync();
            if (categories == null || !categories.Any())
                return new ErrorResult("Bulunamayan kategori var.");
            categories.ForEach(x => x.IsFavorite = true);
            await dbContext.SaveChangesAsync();
            return new SuccessResult("Kategoriler başarı ile favorilere eklendi.");
        }

        public async Task<IDataResult<List<CategoryDetailDto>>> GetFavoriteCategories()
        {
            using var dbContext = contextFactory.CreateDbContext();
            var result = await dbContext.CategorySummaries
                .Where(x => x.IsFavorite)
                .OrderByDescending(x => x.CategoryId)
                .Select(x => new CategoryDetailDto(
                    x.CategoryId, x.ProductCount, x.Name,
                    x.SubCategoryCount, x.IsFavorite, x.AttributeCount, x.SuperCategoryName!))
                .ToListAsync();
            return new SuccessDataResult<List<CategoryDetailDto>>(result);
        }

        public async Task<IDataResult<List<CategoryDetailDto>>> GetSubCategories()
        {
            using var dbContext = contextFactory.CreateDbContext();
            var result = await dbContext.CategorySummaries
                .Where(x => x.SubCategoryCount == 0)
                .OrderByDescending(x => x.IsFavorite).ThenBy(x => x.Name)
                .Select(x => new CategoryDetailDto(
                    x.CategoryId, x.ProductCount, x.Name,
                    x.SubCategoryCount, x.IsFavorite, x.AttributeCount, x.SuperCategoryName!))
                .ToListAsync();
            return new SuccessDataResult<List<CategoryDetailDto>>(result);
        }

        public async Task<IDataResult<List<CategoryDetailDto>>> GetSuperCategories()
        {
            using var dbContext = contextFactory.CreateDbContext();
            var result = await dbContext.CategorySummaries
                .Where(x => x.AttributeCount == 0)
                .OrderByDescending(x => x.IsFavorite).ThenBy(x => x.Name)
                .Select(x => new CategoryDetailDto(
                    x.CategoryId, x.ProductCount, x.Name,
                    x.SubCategoryCount, x.IsFavorite, x.AttributeCount, x.SuperCategoryName!))
                .ToListAsync();
            return new SuccessDataResult<List<CategoryDetailDto>>(result);
        }

        public async Task<IDataResult<Category>> GetCategoryEditDetail(int id)
        {
            using var dbContext = contextFactory.CreateDbContext();
            var result = await dbContext.Categories.FirstOrDefaultAsync(x => x.Id == id);
            return new SuccessDataResult<Category>(result!);
        }

        public async Task<bool> Exits(int id)
        {
            if (id <= 0) return false;
            using var dbContext = contextFactory.CreateDbContext();
            return await dbContext.Categories.AnyAsync(x => x.Id == id);
        }

        public async Task<string?> GetCategoryNameById(int categoryId)
        {
            using var dbContext = contextFactory.CreateDbContext();
            return await dbContext.Categories.AsNoTracking().Where(x => x.Id == categoryId).Select(x => x.Name).FirstOrDefaultAsync();
        }

        public async Task<Category?> GetCategoryById(int? categoryId)
        {
            using var dbContext = contextFactory.CreateDbContext();
            return await dbContext.Categories.FirstOrDefaultAsync(x => x.Id == categoryId);
        }

        public async Task<Category?> GetCategoryWithAttrById(int? categoryId)
        {
            using var dbContext = contextFactory.CreateDbContext();
            return await dbContext.Categories.Include(x => x.CategoryAttributes).FirstOrDefaultAsync(x => x.Id == categoryId);
        }

        public async Task UpdatePlainCategory(Category category)
        {
            using var dbContext = contextFactory.CreateDbContext();
            dbContext.Categories.Update(category);
            await dbContext.SaveChangesAsync();
        }

        public async Task<bool> IsSuper(int? categoryId)
        {
            using var dbContext = contextFactory.CreateDbContext();
            return await dbContext.Categories.AnyAsync(c => c.Id == categoryId && c.SubCategories.Any());
        }

        public async Task<List<Category>> GetAllCategoriesWithHierarchyAsync()
        {
            using var dbContext = contextFactory.CreateDbContext();
            return await dbContext.Categories
                .AsSingleQuery()
                .Include(c => c.CategoryAttributes).ThenInclude(ca => ca.CategoryAttribute)
                .Include(c => c.MarketplaceLinks).ThenInclude(ml => ml.MarketPlace)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Category?> GetCategoryDetailById(int categoryId)
        {
            using var dbContext = contextFactory.CreateDbContext();
            return await dbContext.Categories
                .Include(c => c.SuperCategory)
                .Include(c => c.SubCategories)
                .Include(c => c.CategoryAttributes).ThenInclude(ca => ca.CategoryAttribute)
                .FirstOrDefaultAsync(c => c.Id == categoryId);
        }

        public async Task<List<Category>> GetAllCategoriesWithoutAttributesAsync()
        {
            if (cache.TryGetValue(CategoryListCacheKey, out List<Category>? cached) && cached is not null)
                return cached;

            using var dbContext = contextFactory.CreateDbContext();
            var result = await dbContext.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
            cache.Set(CategoryListCacheKey, result, TimeSpan.FromMinutes(30));
            return result;
        }

        public async Task<bool> IsLeafCategoryAsync(int categoryId)
        {
            using var dbContext = contextFactory.CreateDbContext();
            return !await dbContext.Categories.AsNoTracking().AnyAsync(c => c.SuperCategoryId == categoryId && !c.IsDeleted);
        }

        public async Task<List<Category>> GetValidParentCandidatesAsync()
        {
            using var dbContext = contextFactory.CreateDbContext();
            return await dbContext.Categories
                .AsNoTracking()
                .Where(c => !c.CategoryAttributes.Any())
                .Where(c => !c.MarketplaceLinks.Any(m => m.IsActive))
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<List<Category>> GetValidParentCandidatesAsync(int? excludeCategoryId)
        {
            if (excludeCategoryId is null or <= 0)
                return await GetValidParentCandidatesAsync();

            using var dbContext = contextFactory.CreateDbContext();

            var descendantIds = await GetDescendantIdsAsync(dbContext, excludeCategoryId.Value);
            descendantIds.Add(excludeCategoryId.Value);

            return await dbContext.Categories
                .AsNoTracking()
                .Where(c => !descendantIds.Contains(c.Id))
                .Where(c => !c.CategoryAttributes.Any())
                .Where(c => !c.MarketplaceLinks.Any(m => m.IsActive))
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<List<Category>> GetLeafCategoriesAsync()
        {
            using var dbContext = contextFactory.CreateDbContext();
            return await dbContext.Categories
                .AsSingleQuery()
                .AsNoTracking()
                .Where(c => !c.SubCategories.Any())
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task RefreshCategorySummaryAsync()
        {
            using var dbContext = contextFactory.CreateDbContext();
            await dbContext.Database.ExecuteSqlRawAsync(
                "REFRESH MATERIALIZED VIEW CONCURRENTLY mv_category_summary");
        }

        public async Task<List<CategoryMarketplace>> GetCategoryMarketplaceLinksAsync(int categoryId)
        {
            using var dbContext = contextFactory.CreateDbContext();
            return await dbContext.Categories
                .AsNoTracking()
                .Where(c => c.Id == categoryId)
                .SelectMany(c => c.MarketplaceLinks)
                .Include(cm => cm.MarketPlace)
                .ToListAsync();
        }

        public async Task<IDataResult<CategoryEditPageDto>> GetCategoryEditPageData(int categoryId)
        {
            using var dbContext = contextFactory.CreateDbContext();
            // 1. Kategori bilgisini direkt ID ile çek
            var category = await dbContext.Categories
                .FirstOrDefaultAsync(c => c.Id == categoryId);
            if (category is null)
                return new ErrorDataResult<CategoryEditPageDto>(null!, Messages.CategoryNotFound);

            var isLeaf = !await dbContext.Categories
                .AnyAsync(c => c.SuperCategoryId == categoryId && !c.IsDeleted);

            var descendantIds = await GetDescendantIdsAsync(dbContext, categoryId);
            descendantIds.Add(categoryId);
            var categoriesWithAttributes = await dbContext.CategoryAttributeCategories
                .Select(cac => cac.CategoryId)
                .Distinct()
                .ToListAsync();
            var categoriesWithAttributesSet = categoriesWithAttributes.ToHashSet();

            // validParents — full entity gerekli çünkü DTO'ya Category nesnesi veriliyor
            var validParents = await dbContext.Categories
                .Where(c => !descendantIds.Contains(c.Id))
                .Where(c => !categoriesWithAttributesSet.Contains(c.Id))
                .OrderBy(c => c.Name)
                .ToListAsync();

            // 2. Tüm attribute'lar + bu kategorinin attribute'ları — tek CategoryAttributes sorgusu
            var allAttributes = await dbContext.CategoryAttributes
                .Include(x => x.CategoryAttributeValues)
                .ToListAsync();

            var categoryAttrDtos = isLeaf
                ? await dbContext.CategoryAttributes
                    .Where(x => x.Categories.Any(c => c.CategoryId == categoryId))
                    .Select(x => new CategoryAttributeDto(
                        x.Id,
                        x.Categories.First(z => z.CategoryId == categoryId).IsRequired,
                        x.AllowCustom,
                        x.Categories.First(z => z.CategoryId == categoryId).IsVarianter,
                        x.Categories.First(z => z.CategoryId == categoryId).IsSlicer,
                        x.CreatedAt,
                        x.CategoryAttributeKey!,
                        x.CategoryAttributeHumanized!,
                        x.CategoryAttributeValues.ToList()))
                    .ToListAsync()
                : [];

            // 3. Ürün/satış durumu — tek sorgu ile
            var hasSold = await dbContext.SaleItems
                .AnyAsync(si => si.ProductVariant.Product.CategoryId == categoryId);
            var hasProducts = hasSold || await dbContext.MainProducts
                .AnyAsync(p => p.CategoryId == categoryId);

            return new SuccessDataResult<CategoryEditPageDto>(new CategoryEditPageDto(
                Category: category,
                IsLeaf: isLeaf,
                CategoryAttributes: categoryAttrDtos,
                AllAttributes: allAttributes,
                ValidParentCandidates: validParents,
                HasSoldProducts: hasSold,
                HasProducts: hasProducts));
        }

        public async Task<IDataResult<Category>> GetCategoryBySeoSlugAsync(string slug)
        {
            using var dbContext = contextFactory.CreateDbContext();
            var category = await dbContext.Categories
                .Include(c => c.SubCategories)
                .Include(c => c.SuperCategory)
                .FirstOrDefaultAsync(c => c.SeoSlug == slug && !c.IsDeleted);

            if (category is null)
                return new ErrorDataResult<Category>(null!, "Kategori bulunamadı.");

            return new SuccessDataResult<Category>(category);
        }

        public async Task<IDataResult<List<CategoryTreeDto>>> GetCategoryTreeAsync()
        {
            using var dbContext = contextFactory.CreateDbContext();

            var allCategories = await dbContext.Categories
                .Where(c => !c.IsDeleted)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.SeoSlug,
                    c.SuperCategoryId,
                    ProductCount = c.Products.Count()
                })
                .ToListAsync();

            CategoryTreeDto BuildNode(int id, string name, string? seoSlug, int productCount)
            {
                var children = allCategories
                    .Where(c => c.SuperCategoryId == id)
                    .Select(c => BuildNode(c.Id, c.Name, c.SeoSlug, c.ProductCount))
                    .ToList();
                return new CategoryTreeDto(id, name, seoSlug, productCount, children);
            }

            var roots = allCategories
                .Where(c => c.SuperCategoryId == null)
                .Select(c => BuildNode(c.Id, c.Name, c.SeoSlug, c.ProductCount))
                .ToList();

            return new SuccessDataResult<List<CategoryTreeDto>>(roots);
        }

        private static async Task<HashSet<int>> GetDescendantIdsAsync(IntegrationDbContext dbContext, int categoryId)
        {
            var allPairs = await dbContext.Categories
                .Select(c => new { c.Id, c.SuperCategoryId })
                .ToListAsync();

            var childrenMap = allPairs
                .Where(c => c.SuperCategoryId.HasValue)
                .GroupBy(c => c.SuperCategoryId!.Value)
                .ToDictionary(g => g.Key, g => g.Select(c => c.Id).ToList());

            var descendants = new HashSet<int>();
            var queue = new Queue<int>();
            queue.Enqueue(categoryId);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (childrenMap.TryGetValue(current, out var children))
                    foreach (var childId in children)
                        if (descendants.Add(childId))
                            queue.Enqueue(childId);
            }
            return descendants;
        }
    }
}

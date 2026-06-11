using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.Business.Utilities;
using Entegrasyon.Entity.Results;
using System.Collections.Immutable;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Logs;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete;

public class CategoryAttributeCategoryManager : ICategoryAttributeCategoryManager
{
    private readonly IDbContextFactory<IntegrationDbContext> _contextFactory;
    private readonly IApplicationLogManager _applicationLogManager;
    private readonly ILogger<CategoryAttributeCategoryManager> _logger;

    public CategoryAttributeCategoryManager(
        IDbContextFactory<IntegrationDbContext> contextFactory,
        IApplicationLogManager applicationLogManager,
        ILogger<CategoryAttributeCategoryManager> logger)
    {
        _contextFactory = contextFactory;
        _applicationLogManager = applicationLogManager;
        _logger = logger;
    }

    public async Task<IResult> AddCategoryAttributeForCategory(int catId, IEnumerable<AddCategoryAttributeDto> dto)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync();
        var dtoList = dto.ToList();

        if (dtoList.Count(d => d.IsVarianter) > 1)
            return new ErrorResult("Bir kategoride en fazla 1 adet varyant özelliği olabilir.");

        if (dtoList.Count(d => d.IsSlicer) > 1)
            return new ErrorResult("Bir kategoride en fazla 1 adet dilimleyici özellik olabilir.");

        if (dtoList.Any(d => d.IsVarianter && d.IsSlicer))
            return new ErrorResult("Bir özellik hem varyant hem de dilimleyici olamaz.");

        var hasDuplicateIds = dtoList
            .Where(d => d.Id > 0)
            .GroupBy(d => d.Id)
            .Any(g => g.Count() > 1);
        if (hasDuplicateIds)
            return new ErrorResult("Aynı özellik birden fazla kez eklenemez.");

        var hasDuplicateKeys = dtoList
            .Where(d => d.Id == 0 && !string.IsNullOrEmpty(d.CategoryAttributeKey))
            .GroupBy(d => d.CategoryAttributeKey, StringComparer.OrdinalIgnoreCase)
            .Any(g => g.Count() > 1);
        if (hasDuplicateKeys)
            return new ErrorResult("Aynı anahtar ile birden fazla yeni özellik eklenemez.");

        var result = LogicRunner.Run(
            await CategoryExists(dbContext, catId),
            await IsSuper(dbContext, catId));
        if (result != null)
            return result;

        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            // 1. Mevcut junction kayıtlarını sil ve flush et.
            // IgnoreQueryFilters: composite PK (CategoryId, CategoryAttributeId) çakışmasını önlemek için
            // SOFT-DELETE edilmiş (IsDeleted=true) artık satırları da hard-delete et — aksi halde
            // RemoveCategoryAttributeFromCategory ile kaldırılan bir özellik tekrar eklenemez (PK ihlali).
            var deletedOnes = await dbContext.CategoryAttributeCategories
                .IgnoreQueryFilters()
                .Where(x => x.CategoryId == catId)
                .ToListAsync();
            dbContext.CategoryAttributeCategories.RemoveRange(deletedOnes);
            await dbContext.SaveChangesAsync();

            // Tracker'ı temizle — eski entity referansları yeni ekleme ile çakışmasın
            dbContext.ChangeTracker.Clear();

            // 2. Yeni kayıtları oluştur ve ekle
            var existingCatAttrs = await GetExistingCategoryAttributes(dbContext, dtoList);
            var existingCatAttrValues = await GetExistingCategoryAttributeValues(dbContext, dtoList);
            var catAttrCats = dtoList
                .Select(CreateCategoryAttributeCategory(catId, existingCatAttrs, existingCatAttrValues))
                .ToList();
            dbContext.CategoryAttributeCategories.AddRange(catAttrCats);
            await dbContext.SaveChangesAsync();

            await transaction.CommitAsync();
            return new SuccessResult();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    public async Task<IResult> RemoveCategoryAttributeFromCategory(int catId, int categoryAttributeId)
    {
        // 1. Validation — geçerli id'ler
        if (catId <= 0 || categoryAttributeId <= 0)
            return new ErrorResult("Geçersiz kategori veya özellik bilgisi.");

        await using var dbContext = await _contextFactory.CreateDbContextAsync();

        // 2 + 3. Business Rule + Execution tek round-trip ile (TOCTOU yok):
        // junction'ı AsTracking ile çek (global NoTracking → mutasyon için ŞART).
        // FirstOrDefault → null = "bağ yok" (kategori yoksa da bağ olmaz, bu durumu da kapsar).
        var junction = await dbContext.CategoryAttributeCategories
            .AsTracking()
            .FirstOrDefaultAsync(x => x.CategoryId == catId && x.CategoryAttributeId == categoryAttributeId);
        if (junction is null)
            return new ErrorResult("Bu özellik kategoriye bağlı değil.");

        // Soft-delete + kullanıcı log'u tek transaction içinde (AddLog senkron DB write yapıyor —
        // log fail olursa mutasyon da rollback olsun, atomik).
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        junction.IsDeleted = true;
        junction.DeletedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();

        await _applicationLogManager.AddLog(
            "Kategoriden özellik kaldırıldı.", LogType.Category, LogAction.Delete);
        await transaction.CommitAsync();

        _logger.LogInformation(
            "Removed category-attribute binding: CategoryId={CategoryId}, CategoryAttributeId={AttributeId}",
            catId, categoryAttributeId);

        return new SuccessResult("Özellik kategoriden kaldırıldı.");
    }

    private async Task<ImmutableDictionary<int, CategoryAttribute>> GetExistingCategoryAttributes(IntegrationDbContext dbContext, IEnumerable<AddCategoryAttributeDto> dto)
    {
        var ids = dto.Where(d => d.Id > 0).Select(d => d.Id).ToList();
        if (ids.Count == 0) return ImmutableDictionary<int, CategoryAttribute>.Empty;
        return (await dbContext.CategoryAttributes.AsTracking().Where(ca => ids.Contains(ca.Id))
            .ToListAsync()).ToImmutableDictionary(ca => ca.Id);
    }

    private async Task<List<CategoryAttributeValue>> GetExistingCategoryAttributeValues(IntegrationDbContext dbContext, IEnumerable<AddCategoryAttributeDto> dto)
    {
        var catAttrValueIds = dto.SelectMany(d => d.CategoryAttributeValues)
            .Where(v => v.Id > 0)
            .Select(d => d.Id)
            .ToArray();
        if (catAttrValueIds.Length == 0) return [];
        return await dbContext.CategoryAttributeValues.AsTracking()
            .Where(cav => catAttrValueIds.Contains(cav.Id))
            .ToListAsync();
    }
    private static Func<AddCategoryAttributeDto, CategoryAttributeCategory> CreateCategoryAttributeCategory(int catId, ImmutableDictionary<int, CategoryAttribute> existingCatAttrs, List<CategoryAttributeValue> existingCatAttrValues)
    {
        var valuesById = existingCatAttrValues.ToDictionary(v => v.Id);

        return catAttr =>
        {
            var catAttrcat = new CategoryAttributeCategory()
            {
                IsRequired = catAttr.IsRequired,
                IsSlicer = catAttr.IsSlicer,
                IsVarianter = catAttr.IsVarianter,
                CategoryId = catId,
            };
            if (catAttr.Id != 0)
            {
                var existingCatAttr = existingCatAttrs.GetValueOrDefault(catAttr.Id);
                existingCatAttr!.CategoryAttributeHumanized = catAttr.CategoryAttributeHumanized;
                existingCatAttr.CategoryAttributeKey = catAttr.CategoryAttributeKey;
                existingCatAttr.AllowCustom = catAttr.AllowCustom;
                catAttrcat.CategoryAttribute = existingCatAttr;
            }
            else
            {
                catAttrcat.CategoryAttribute = new CategoryAttribute
                {
                    Id = catAttr.Id,
                    CategoryAttributeKey = catAttr.CategoryAttributeKey,
                    CategoryAttributeHumanized = catAttr.CategoryAttributeHumanized,
                    AllowCustom = catAttr.AllowCustom,
                    CategoryAttributeValues = new()
                };
            }
            foreach (var cav in catAttr.CategoryAttributeValues)
            {
                catAttrcat.CategoryAttribute.CategoryAttributeValues
                    .Add(valuesById.TryGetValue(cav.Id, out var existing) ? existing : cav);
            }
            return catAttrcat;
        };
    }

    private async Task<IResult> IsSuper(IntegrationDbContext dbContext, int categoryId)
    {
        var isSuper = await dbContext.Categories.AnyAsync(c => c.Id == categoryId && c.SubCategories.Any());
        if (isSuper)
            return new ErrorResult(Messages.CategoryIsSuper);
        return new SuccessResult();
    }
    private async Task<IResult> CategoryExists(IntegrationDbContext dbContext, int categoryId)
    {
        var exits = await dbContext.Categories.AnyAsync(c => c.Id == categoryId);
        if (!exits)
            return new ErrorResult(Messages.CategoryNotFound);
        return new SuccessResult();
    }
}

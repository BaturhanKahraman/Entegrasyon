using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Categories;
using Entegrasyon.Entity.Dtos.Category;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.Business.Utilities;
using Entegrasyon.Entity.Results;
using System.Collections.Immutable;
using Entegrasyon.Business.Abstract;

namespace Entegrasyon.Business.Concrete;

public class CategoryAttributeCategoryManager : ICategoryAttributeCategoryManager
{
    private readonly IntegrationDbContext _ctx;
    public CategoryAttributeCategoryManager(IntegrationDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<IResult> AddCategoryAttributeForCategory(int catId, IEnumerable<AddCategoryAttributeDto> dto)
    {
        var dtoList = dto.ToList();

        if (dtoList.Count(d => d.IsVarianter) > 1)
            return new ErrorResult("Bir kategoride en fazla 1 adet varyant özelliği olabilir.");

        if (dtoList.Count(d => d.IsSlicer) > 1)
            return new ErrorResult("Bir kategoride en fazla 1 adet dilimleyici özellik olabilir.");

        if (dtoList.Any(d => d.IsVarianter && d.IsSlicer))
            return new ErrorResult("Bir özellik hem varyant hem de dilimleyici olamaz.");

        var result = LogicRunner.Run(
            await CategoryExists(catId),
            await IsSuper(catId));
        if (result != null)
            return result;

        await using var transaction = await _ctx.Database.BeginTransactionAsync();
        try
        {
            // 1. Mevcut junction kayıtlarını sil ve flush et
            var deletedOnes = await _ctx.CategoryAttributeCategories
                .Where(x => x.CategoryId == catId)
                .ToListAsync();
            _ctx.CategoryAttributeCategories.RemoveRange(deletedOnes);
            await _ctx.SaveChangesAsync();

            // 2. Yeni kayıtları oluştur ve ekle
            var existingCatAttrs = await GetExistingCategoryAttributes(dtoList);
            var existingCatAttrValues = await GetExistingCategoryAttributeValues(dtoList);
            var catAttrCats = dtoList
                .Select(CreateCategoryAttributeCategory(catId, existingCatAttrs, existingCatAttrValues))
                .ToList();
            _ctx.CategoryAttributeCategories.AddRange(catAttrCats);
            await _ctx.SaveChangesAsync();

            await transaction.CommitAsync();
            return new SuccessResult();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    private async Task<ImmutableDictionary<int, CategoryAttribute>> GetExistingCategoryAttributes(IEnumerable<AddCategoryAttributeDto> dto)
    {
        var ids = dto.Select(d => d.Id);
        return (await _ctx.CategoryAttributes.AsTracking().Where(ca => ids.Contains(ca.Id))
            .ToListAsync()).ToImmutableDictionary(ca => ca.Id);
    }

    private async Task<List<CategoryAttributeValue>> GetExistingCategoryAttributeValues(IEnumerable<AddCategoryAttributeDto> dto)
    {
        var catAttrValueIds = dto.SelectMany(d => d.CategoryAttributeValues).Select(d => d.Id).ToArray();
        return await _ctx.CategoryAttributeValues.AsTracking()
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
                existingCatAttr.CategoryAttributeHumanized = catAttr.CategoryAttributeHumanized;
                existingCatAttr.CategoryAttributeKey = catAttr.CategoryAttributeKey;
                catAttrcat.CategoryAttribute = existingCatAttr;
            }
            else
            {
                catAttrcat.CategoryAttribute = new CategoryAttribute
                {
                    Id = catAttr.Id,
                    CategoryAttributeKey = catAttr.CategoryAttributeKey,
                    CategoryAttributeHumanized = catAttr.CategoryAttributeHumanized,
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

    private async Task<IResult> IsSuper(int categoryId)
    {
        var isSuper = await _ctx.Categories.AnyAsync(c => c.Id == categoryId && c.SubCategories.Any());
        if (isSuper)
            return new ErrorResult(Messages.CategoryIsSuper);
        return new SuccessResult();
    }
    private async Task<IResult> CategoryExists(int categoryId)
    {
        var exits = await _ctx.Categories.AnyAsync(c => c.Id == categoryId);
        if (!exits)
            return new ErrorResult(Messages.CategoryNotFound);
        return new SuccessResult();
    }
}

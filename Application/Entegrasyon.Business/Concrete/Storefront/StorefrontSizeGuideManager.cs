using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete.Storefront;

public class StorefrontSizeGuideManager(
    IDbContextFactory<IntegrationDbContext> contextFactory) : IStorefrontSizeGuideManager
{
    public async Task<IDataResult<StorefrontSizeGuide?>> GetSizeGuideForCategoryAsync(int tenantId, int categoryId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var guides = await dbContext.StorefrontSizeGuides
            .AsNoTracking()
            .Where(g => g.TenantId == tenantId && g.IsActive)
            .ToListAsync();

        var categoryIdStr = categoryId.ToString();

        var match = guides.FirstOrDefault(g =>
        {
            if (string.IsNullOrWhiteSpace(g.CategoryIds))
                return false;

            try
            {
                var ids = JsonSerializer.Deserialize<List<int>>(g.CategoryIds);
                return ids != null && ids.Contains(categoryId);
            }
            catch
            {
                return false;
            }
        });

        return new SuccessDataResult<StorefrontSizeGuide?>(match);
    }

    public async Task<IDataResult<List<StorefrontSizeGuide>>> GetAllSizeGuidesAsync(int tenantId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var guides = await dbContext.StorefrontSizeGuides
            .AsNoTracking()
            .Where(g => g.TenantId == tenantId)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();

        return new SuccessDataResult<List<StorefrontSizeGuide>>(guides);
    }

    public async Task<IResult> CreateOrUpdateAsync(StorefrontSizeGuide guide)
    {
        if (string.IsNullOrWhiteSpace(guide.Name))
            return new ErrorResult("Beden rehberi adi zorunludur.");

        if (string.IsNullOrWhiteSpace(guide.SizeData))
            return new ErrorResult("Beden verileri zorunludur.");

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        if (guide.Id > 0)
        {
            var existing = await dbContext.StorefrontSizeGuides
                .FirstOrDefaultAsync(g => g.Id == guide.Id);

            if (existing is null)
                return new ErrorResult("Beden rehberi bulunamadi.");

            existing.Name = guide.Name;
            existing.CategoryIds = guide.CategoryIds;
            existing.MeasurementImageUrl = guide.MeasurementImageUrl;
            existing.MeasurementInstructions = guide.MeasurementInstructions;
            existing.SizeData = guide.SizeData;
            existing.IsActive = guide.IsActive;

            dbContext.StorefrontSizeGuides.Update(existing);
        }
        else
        {
            dbContext.StorefrontSizeGuides.Add(guide);
        }

        await dbContext.SaveChangesAsync();
        return new SuccessResult("Beden rehberi basariyla kaydedildi.");
    }

    public async Task<IResult> DeleteAsync(int id)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var guide = await dbContext.StorefrontSizeGuides
            .FirstOrDefaultAsync(g => g.Id == id);

        if (guide is null)
            return new ErrorResult("Beden rehberi bulunamadi.");

        guide.IsDeleted = true;
        guide.DeletedAt = DateTimeOffset.UtcNow;
        dbContext.StorefrontSizeGuides.Update(guide);
        await dbContext.SaveChangesAsync();

        return new SuccessResult("Beden rehberi silindi.");
    }
}

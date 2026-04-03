using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity;
using Entegrasyon.Business.Mappers;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Requests;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.Business.Extensions;
using Entegrasyon.Business.Tenants;
using Entegrasyon.Business.Utilities;
using Entegrasyon.Entity.Results;
using Microsoft.Extensions.Caching.Hybrid;

namespace Entegrasyon.Business.Concrete;

public class BrandService(IFluentValidator validator, IApplicationLogManager applicationLogManager, BrandMapper mapper, IDbContextFactory<IntegrationDbContext> contextFactory, TenantMemoryCache cache, HybridCache hybridCache, ITenantContext tenantContext)
    : IBrandService
{
    private const string brandListCacheKey = "brands:list";

    public async Task<IDataResult<List<BrandListDetailDto>>> GetBrandListDetails()
    {
        var result = await hybridCache.GetOrCreateAsync(
            $"t:{tenantContext.TenantId}:{brandListCacheKey}",
            async ct =>
            {
                await using var dbContext = await contextFactory.CreateDbContextAsync(ct);
                return await dbContext.Brands
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(x => new BrandListDetailDto(x.Id, x.CreatedAt, x.Name, x.Products.Count()))
                    .ToListAsync(ct);
            },
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(30),
                LocalCacheExpiration = TimeSpan.FromMinutes(10)
            },
            tags: ["brands"]);

        return new SuccessDataResult<List<BrandListDetailDto>>(result);
    }

    public async Task<IResult> AddBrand(AddBrandDto brandDto)
    {
        await applicationLogManager.AddLog("Marka ekleme istegi geldi.", LogType.Brand, LogAction.Add, brandDto);
        await validator.ValidateAndThrowAsync(brandDto);

        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var brand = mapper.MapToEntity(brandDto);
        var result = LogicRunner.Run(await CheckIfTheSameNameExits(dbContext, brand.Name));
        if (result != null)
        {
            await applicationLogManager.AddLog($"Marka eklenemedi. {result.Message}", LogType.Brand, LogAction.Add, brandDto);
            return new ErrorResult(result.Message!);
        }
        dbContext.Brands.Add(brand);
        await dbContext.SaveChangesAsync();
        cache.Remove(brandListCacheKey);
        await hybridCache.RemoveByTagAsync("brands");
        await applicationLogManager.AddLog("Marka basariyla eklendi.", LogType.Brand, LogAction.Add, brandDto);
        return new SuccessDataResult<Brand>(brand);
    }

    public async Task<IResult> UpdateBrand(Brand brand)
    {
        await applicationLogManager.AddLog("Marka guncelleme istegi geldi.", LogType.Brand, LogAction.Update, brand);

        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var result = LogicRunner.Run(await CheckIfTheSameNameExits(dbContext, brand.Name));
        if (result != null)
        {
            await applicationLogManager.AddLog($"Marka guncellenemedi. {result.Message}", LogType.Brand, LogAction.Add, brand);
            return new ErrorResult(result.Message!);
        }
        await validator.ValidateAndThrowAsync(brand);
        dbContext.Brands.Update(brand);
        await dbContext.SaveChangesAsync();
        cache.Remove(brandListCacheKey);
        await hybridCache.RemoveByTagAsync("brands");
        await applicationLogManager.AddLog("Marka basariyla guncellendi.", LogType.Brand, LogAction.Update, brand);
        var detail = await dbContext.Brands
            .Where(x => x.Id == brand.Id)
            .Select(x => new BrandListDetailDto(x.Id, x.CreatedAt, x.Name, x.Products.Count()))
            .FirstOrDefaultAsync();
        return new SuccessDataResult<BrandListDetailDto>(detail!, Messages.BrandUpdatedSuccessfuly);
    }

    public async Task<IResult> DeleteBrand(int id)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var brand = await dbContext.Brands.AsTracking().FirstOrDefaultAsync(x => x.Id == id);
        await applicationLogManager.AddLog("Marka silme istegi geldi.", LogType.Brand, LogAction.Delete, brand);
        if (brand == null)
        {
            await applicationLogManager.AddLog("Marka silme basarisiz. Ilgili id bulunamadi:", LogType.Brand, LogAction.Delete, id);
            return new ErrorResult("Boyle bir marka bulunamadi");
        }
        brand.IsDeleted = true;
        brand.DeletedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();
        cache.Remove(brandListCacheKey);
        await hybridCache.RemoveByTagAsync("brands");
        await applicationLogManager.AddLog("Marka basariyla silindi.", LogType.Brand, LogAction.Delete, brand);
        return new SuccessResult();
    }

    public async Task<IDataResult<Pageable<BrandListDetailDto>>> GetBrandDetailPageable(BrandDetailPaginatedRequest request)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        IQueryable<Brand> query = dbContext.Brands;
        query = query.ApplyGlobalSearch(request.SearchTerm, nameof(Brand.Name));

        var items = await query
            .Select(x => new BrandListDetailDto(x.Id, x.CreatedAt, x.Name, x.Products.Count()))
            .ToPageableAsync(request);

        return new SuccessDataResult<Pageable<BrandListDetailDto>>(items);
    }

    public async Task<IDataResult<Brand>> GetBrandById(int id)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        return new SuccessDataResult<Brand>((await dbContext.Brands.FindAsync(id))!);
    }

    public async Task<IDataResult<BrandDetailDto>> GetBrandDetail(int id)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var entity = await dbContext.Brands
            .Select(x => new BrandDetailDto(x.Id, x.CreatedAt, x.Name, x.Products.Count()))
            .FirstOrDefaultAsync(x => x.Id == id);
        return new SuccessDataResult<BrandDetailDto>(entity!);
    }

    public async Task<IDataResult<Brand>> GetBrandBySeoSlugAsync(string slug)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var brand = await dbContext.Brands
            .FirstOrDefaultAsync(b => b.SeoSlug == slug && !b.IsDeleted);

        if (brand is null)
            return new ErrorDataResult<Brand>(null!, "Marka bulunamadı.");

        return new SuccessDataResult<Brand>(brand);
    }

    private static async Task<IResult> CheckIfTheSameNameExits(IntegrationDbContext dbContext, string name)
    {
        if (await dbContext.Brands.AnyAsync(x => x.Name == name))
            return new ErrorResult("Bu isimde bir marka zaten mevcut");
        return new SuccessResult();
    }
}

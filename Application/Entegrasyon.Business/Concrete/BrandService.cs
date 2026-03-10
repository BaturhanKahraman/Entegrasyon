using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Requests;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Entegrasyon.Business.Extensions;
using Entegrasyon.Business.Utilities;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Concrete;

public class BrandService(IFluentValidator validator, IApplicationLogManager applicationLogManager, IMapper mapper, IntegrationDbContext dbContext, IMemoryCache cache)
    : IBrandService
{
    private const string brandListCacheKey = "brands:list";

    public async Task<IDataResult<List<BrandListDetailDto>>> GetBrandListDetails()
    {
        if (cache.TryGetValue(brandListCacheKey, out List<BrandListDetailDto> cached))
            return new SuccessDataResult<List<BrandListDetailDto>>(cached);

        var result = await dbContext.Brands
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new BrandListDetailDto(x.Id, x.CreatedAt, x.Name, x.Products.Count()))
            .ToListAsync();

        cache.Set(brandListCacheKey, result, TimeSpan.FromMinutes(30));
        return new SuccessDataResult<List<BrandListDetailDto>>(result);
    }

    public async Task<IResult> AddBrand(AddBrandDto brandDto)
    {
        await applicationLogManager.AddLog("Marka ekleme isteği geldi.", LogType.Brand, LogAction.Add, brandDto);
        await validator.ValidateAndThrowAsync(brandDto);
        var brand = mapper.Map<AddBrandDto, Brand>(brandDto);
        var result = LogicRunner.Run(await CheckIfTheSameNameExits(brand.Name));
        if (result != null)
        {
            await applicationLogManager.AddLog($"Marka eklenemedi. {result.Message}", LogType.Brand, LogAction.Add, brandDto);
            return new ErrorResult(result.Message);
        }
        dbContext.Brands.Add(brand);
        await dbContext.SaveChangesAsync();
        cache.Remove(brandListCacheKey);
        await applicationLogManager.AddLog("Marka başarıyla eklendi.", LogType.Brand, LogAction.Add, brandDto);
        return new SuccessDataResult<Brand>(brand);
    }

    public async Task<IResult> UpdateBrand(Brand brand)
    {
        await applicationLogManager.AddLog("Marka güncelleme isteği geldi.", LogType.Brand, LogAction.Update, brand);
        var result = LogicRunner.Run(await CheckIfTheSameNameExits(brand.Name));
        if (result != null)
        {
            await applicationLogManager.AddLog($"Marka güncellenemedi. {result.Message}", LogType.Brand, LogAction.Add, brand);
            return new ErrorResult(result.Message);
        }
        await validator.ValidateAndThrowAsync(brand);
        dbContext.Brands.Update(brand);
        await dbContext.SaveChangesAsync();
        cache.Remove(brandListCacheKey);
        await applicationLogManager.AddLog("Marka başarıyla güncellendi.", LogType.Brand, LogAction.Update, brand);
        var detail = await dbContext.Brands
            .Where(x => x.Id == brand.Id)
            .Select(x => new BrandListDetailDto(x.Id, x.CreatedAt, x.Name, x.Products.Count()))
            .FirstOrDefaultAsync();
        return new SuccessDataResult<BrandListDetailDto>(detail, Messages.BrandUpdatedSuccessfuly);
    }

    public async Task<IResult> DeleteBrand(int id)
    {
        var brand = await dbContext.Brands.AsTracking().FirstOrDefaultAsync(x => x.Id == id);
        await applicationLogManager.AddLog("Marka silme isteği geldi.", LogType.Brand, LogAction.Delete, brand);
        if (brand == null)
        {
            await applicationLogManager.AddLog("Marka silme başarısız. İlgili id bulunamadı:", LogType.Brand, LogAction.Delete, id);
            return new ErrorResult("Böyle bir marka bulunamadı");
        }
        brand.IsDeleted = true;
        brand.DeletedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();
        cache.Remove(brandListCacheKey);
        await applicationLogManager.AddLog("Marka başarıyla silindi.", LogType.Brand, LogAction.Delete, brand);
        return new SuccessResult();
    }

    public async Task<IDataResult<Pageable<BrandListDetailDto>>> GetBrandDetailPageable(BrandDetailPaginatedRequest request)
    {
        IQueryable<Brand> query = dbContext.Brands;
        query = query.ApplyGlobalSearch(request.SearchTerm, nameof(Brand.Name));

        int total = await query.CountAsync();
        var items = await query
            .Select(x => new BrandListDetailDto(x.Id, x.CreatedAt, x.Name, x.Products.Count()))
            .ToPageableAsync(request);

        return new SuccessDataResult<Pageable<BrandListDetailDto>>(items);
    }

    public async Task<IDataResult<Brand>> GetBrandById(int id) =>
        new SuccessDataResult<Brand>(await dbContext.Brands.FindAsync(id));

    public async Task<IDataResult<BrandDetailDto>> GetBrandDetail(int id)
    {
        var entity = await dbContext.Brands
            .Select(x => new BrandDetailDto(x.Id, x.CreatedAt, x.Name, x.Products.Count()))
            .FirstOrDefaultAsync(x => x.Id == id);
        return new SuccessDataResult<BrandDetailDto>(entity);
    }

    private async Task<IResult> CheckIfTheSameNameExits(string name)
    {
        if (await dbContext.Brands.AnyAsync(x => x.Name == name))
            return new ErrorResult("Bu isimde bir marka zaten mevcut");
        return new SuccessResult();
    }
}

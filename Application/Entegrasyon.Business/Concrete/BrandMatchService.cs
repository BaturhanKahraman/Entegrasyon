using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Extensions;
using Entegrasyon.Business.Utilities;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Matches;
using Entegrasyon.Entity.Results;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Entegrasyon.Business.Concrete;

/// <summary>
/// Brand ve Marketplace brand'lari arasindaki mapping islemlerini yonetir.
/// Trendyol gibi e-ticaret platformlari ile brand eslestirmelerini saglar.
/// </summary>
public class BrandMatchService(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IFluentValidator validator,
    IApplicationLogManager applicationLogManager,
    IMapper mapper) : IBrandMatchService
{
    public async Task AddRange(List<BrandMarketPlaceMatch> entities)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        dbContext.BrandMarketPlaceMatches.AddRange(entities);
        await dbContext.SaveChangesAsync();
    }

    public async Task<List<int>> GetMarketPlaceBrandIdsByMarketPlaceId(int marketPlaceId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        return await dbContext.BrandMarketPlaceMatches
            .Where(x => x.MarketPlaceId == marketPlaceId)
            .Select(x => x.MarketPlaceBrandId)
            .ToListAsync();
    }

    public async Task<BrandMappingSummaryDto> GetBrandMappingsSummaryAsync()
    {
        const int trendyolMarketPlaceId = 1;

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var totalBrands = await dbContext.Brands
            .Where(x => !x.IsDeleted)
            .CountAsync();

        var mappedBrands = await dbContext.BrandMarketPlaceMatches
            .Where(x => x.MarketPlaceId == trendyolMarketPlaceId)
            .Select(x => x.ApplicationBrandId)
            .Distinct()
            .CountAsync();

        return new BrandMappingSummaryDto
        {
            TotalBrands = totalBrands,
            MappedBrands = mappedBrands,
            UnmappedBrands = totalBrands - mappedBrands
        };
    }

    public async Task<List<BrandMarketPlaceMatchDto>> GetAllBrandMappingsAsync(int marketPlaceId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var mappings = await dbContext.BrandMarketPlaceMatches
            .Where(x => x.MarketPlaceId == marketPlaceId)
            .Include(x => x.ApplicationBrand)
            .ToListAsync();

        return mapper.Map<List<BrandMarketPlaceMatch>, List<BrandMarketPlaceMatchDto>>(mappings);
    }

    public async Task<IDataResult<List<BrandMarketPlaceMatchDto>>> GetBrandMappingsByBrandIdAsync(int brandId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var mappings = await dbContext.BrandMarketPlaceMatches
            .Include(m => m.ApplicationBrand)
            .Where(m => m.ApplicationBrandId == brandId)
            .ToListAsync();

        var dtos = mapper.Map<List<BrandMarketPlaceMatch>, List<BrandMarketPlaceMatchDto>>(mappings);
        return new SuccessDataResult<List<BrandMarketPlaceMatchDto>>(dtos);
    }

    public async Task<List<BrandDto>> GetUnmappedBrandsAsync(int marketPlaceId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var mappedBrandIds = await dbContext.BrandMarketPlaceMatches
            .Where(x => x.MarketPlaceId == marketPlaceId)
            .Select(x => x.ApplicationBrandId)
            .ToListAsync();

        var unmappedBrands = await dbContext.Brands
            .Where(x => !x.IsDeleted && !mappedBrandIds.Contains(x.Id))
            .Select(x => new BrandDto { Id = x.Id, Name = x.Name })
            .OrderBy(x => x.Name)
            .ToListAsync();

        return unmappedBrands;
    }

    public async Task<IResult> CreateBrandMappingAsync(CreateBrandMarketPlaceMatchDto dto)
    {
        await applicationLogManager.AddLog("Brand mapping olusturma istegi", LogType.Brand, LogAction.Add, dto);

        // Validation
        await validator.ValidateAndThrowAsync(dto);

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        // Business Rules
        var brand = await dbContext.Brands.FindAsync(dto.ApplicationBrandId);
        if (brand == null || brand.IsDeleted)
        {
            var error = "Secilen marka bulunamadi veya silinmis durumda.";
            await applicationLogManager.AddLog(error, LogType.Brand, LogAction.Add, dto);
            return new ErrorResult(error);
        }

        var existingMapping = await dbContext.BrandMarketPlaceMatches
            .FirstOrDefaultAsync(x =>
                x.ApplicationBrandId == dto.ApplicationBrandId &&
                x.MarketPlaceId == dto.MarketPlaceId);

        if (existingMapping != null)
        {
            var error = "Bu marka icin zaten bir mapping mevcuttur.";
            await applicationLogManager.AddLog(error, LogType.Brand, LogAction.Add, dto);
            return new ErrorResult(error);
        }

        // Execution
        var mapping = new BrandMarketPlaceMatch
        {
            ApplicationBrandId = dto.ApplicationBrandId,
            MarketPlaceId = dto.MarketPlaceId,
            MarketPlaceBrandId = dto.MarketPlaceBrandId
        };

        dbContext.BrandMarketPlaceMatches.Add(mapping);
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog("Brand mapping basariyla olusturuldu", LogType.Brand, LogAction.Add, dto);
        return new SuccessResult("Brand mapping basariyla olusturuldu.");
    }

    public async Task<IResult> RemoveBrandMappingAsync(int brandId, int marketPlaceId)
    {
        await applicationLogManager.AddLog($"Brand mapping silme istegi (BrandId: {brandId})", LogType.Brand, LogAction.Delete);

        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var mapping = await dbContext.BrandMarketPlaceMatches
            .FirstOrDefaultAsync(x =>
                x.ApplicationBrandId == brandId &&
                x.MarketPlaceId == marketPlaceId);

        if (mapping == null)
        {
            var error = "Mapping bulunamadi.";
            await applicationLogManager.AddLog(error, LogType.Brand, LogAction.Delete);
            return new ErrorResult(error);
        }

        dbContext.BrandMarketPlaceMatches.Remove(mapping);
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog("Brand mapping basariyla silindi", LogType.Brand, LogAction.Delete);
        return new SuccessResult("Brand mapping basariyla silindi.");
    }

    public async Task<IResult> ImportTrendyolBrandsAsync()
    {
        // TODO: Implement Trendyol brand import logic
        await Task.CompletedTask;
        return new ErrorResult("Trendyol brand import islevi henuz implement edilmemistir.");
    }

    public async Task<IResult> SyncWithTrendyolAsync()
    {
        // TODO: Implement Trendyol sync logic
        await Task.CompletedTask;
        return new ErrorResult("Trendyol sync islevi henuz implement edilmemistir.");
    }
}

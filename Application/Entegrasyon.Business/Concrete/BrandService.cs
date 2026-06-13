using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Channels.Events.Brands;
using Entegrasyon.Business.Extensions;
using Entegrasyon.Business.FeatureFlags;
using Entegrasyon.Business.Mappers;
using Entegrasyon.Business.Tenants;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.Business.Utilities;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Requests;
using Entegrasyon.Entity.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Entegrasyon.Business.Concrete;

public class BrandService(IFluentValidator validator, IApplicationLogManager applicationLogManager, BrandMapper mapper, IDbContextFactory<IntegrationDbContext> contextFactory, TenantMemoryCache cache, HybridCache hybridCache, ITenantContext tenantContext, IOptions<NotificationFeatureFlags> notificationFlags, ICurrentUserContext currentUser, ILogger<BrandService> logger)
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
        brand.NormalizedName = NormalizeName(brand.Name);
        var result = LogicRunner.Run(await CheckIfTheSameNameExits(dbContext, brand.NormalizedName));
        if (result != null)
        {
            await applicationLogManager.AddLog($"Marka eklenemedi. {result.Message}", LogType.Brand, LogAction.Add, brandDto);
            return new ErrorResult(result.Message!);
        }
        dbContext.Brands.Add(brand);
        if (notificationFlags.Value.PublishEnabled)
        {
            dbContext.AddDomainEvent(new BrandAddedEvent(
                brand.Id,
                brand.Name ?? string.Empty,
                currentUser.UserId ?? Guid.Empty));
        }
        await dbContext.SaveChangesAsync();
        cache.Remove(brandListCacheKey);
        await hybridCache.RemoveByTagAsync("brands");
        await applicationLogManager.AddLog("Marka basariyla eklendi.", LogType.Brand, LogAction.Add, brandDto);
        return new SuccessDataResult<Brand>(brand);
    }

    public async Task<IResult> UpdateBrand(EditBrandDto dto)
    {
        // 1) Validation
        await applicationLogManager.AddLog("Marka guncelleme istegi geldi.", LogType.Brand, LogAction.Update, dto);
        await validator.ValidateAndThrowAsync(dto);

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        // Global no-tracking → mutasyon icin AsTracking ZORUNLU (sessiz no-op footgun).
        var brand = await dbContext.Brands.AsTracking().FirstOrDefaultAsync(x => x.Id == dto.Id);
        if (brand == null)
        {
            await applicationLogManager.AddLog("Marka guncellenemedi. Ilgili marka bulunamadi.", LogType.Brand, LogAction.Update, dto);
            logger.LogWarning("UpdateBrand failed: brand {BrandId} not found", dto.Id);
            return new ErrorResult("Boyle bir marka bulunamadı");
        }

        // 2) Business Rules — DB unique kisitlariyla (IX_Brands_NormalizedName, IX_Brands_SeoSlug)
        //    ortusen, self-exclude'lu app-katmani kontrolleri. Aksi halde SaveChanges DbUpdateException
        //    firlatir ve kullaniciya PRG-hata yerine 500 doner.
        var normalizedName = NormalizeName(dto.Name);
        var result = LogicRunner.Run(await CheckIfTheSameNameExits(dbContext, normalizedName, dto.Id));
        if (result != null)
        {
            await applicationLogManager.AddLog($"Marka guncellenemedi. {result.Message}", LogType.Brand, LogAction.Update, dto);
            logger.LogWarning("UpdateBrand failed for brand {BrandId}: {Reason}", dto.Id, result.Message);
            return new ErrorResult(result.Message!);
        }

        // SeoSlug benzersizligi — bos slug filtered index'te coklanabildiginden kontrolu atla.
        if (!string.IsNullOrWhiteSpace(dto.SeoSlug))
        {
            var slugResult = LogicRunner.Run(await CheckIfSeoSlugExists(dbContext, dto.SeoSlug, dto.Id));
            if (slugResult != null)
            {
                await applicationLogManager.AddLog($"Marka guncellenemedi. {slugResult.Message}", LogType.Brand, LogAction.Update, dto);
                logger.LogWarning("UpdateBrand failed for brand {BrandId}: {Reason}", dto.Id, slugResult.Message);
                return new ErrorResult(slugResult.Message!);
            }
        }

        // 3) Execution — Name ile birlikte NormalizedName'i de tutarli set et (index/arama bayatlamaz).
        brand.Name = dto.Name;
        brand.NormalizedName = normalizedName;
        brand.SeoSlug = dto.SeoSlug;
        if (notificationFlags.Value.PublishEnabled)
        {
            dbContext.AddDomainEvent(new BrandUpdatedEvent(
                brand.Id,
                brand.Name ?? string.Empty,
                Array.Empty<string>(),
                currentUser.UserId ?? Guid.Empty));
        }
        await dbContext.SaveChangesAsync();
        cache.Remove(brandListCacheKey);
        await hybridCache.RemoveByTagAsync("brands");
        await applicationLogManager.AddLog("Marka basariyla guncellendi.", LogType.Brand, LogAction.Update, dto);
        logger.LogInformation("Brand {BrandId} updated", brand.Id);

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
            await applicationLogManager.AddLog("Marka silme başarısız. Ilgili id bulunamadı:", LogType.Brand, LogAction.Delete, id);
            return new ErrorResult("Boyle bir marka bulunamadı");
        }
        var deletedName = brand.Name ?? string.Empty;
        brand.IsDeleted = true;
        brand.DeletedAt = DateTimeOffset.UtcNow;
        if (notificationFlags.Value.PublishEnabled)
        {
            dbContext.AddDomainEvent(new BrandDeletedEvent(
                brand.Id,
                deletedName,
                currentUser.UserId ?? Guid.Empty));
        }
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
            .OrderByDescending(x => x.Products.Count())
            .ThenBy(x => x.Name)
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
            .Where(x => x.Id == id)
            .Select(x => new BrandDetailDto(x.Id, x.CreatedAt, x.Name, x.Products.Count(), x.SeoSlug, x.NormalizedName))
            .FirstOrDefaultAsync();
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

    public async Task<BrandKpiDto> GetBrandKpisAsync(CancellationToken ct = default)
    {
        // Markalar liste sayfası üst KPI kartları. 3 bağımsız index-backed skaler sayım;
        // hepsi DbContext default no-tracking + soft-delete query filter (!IsDeleted) altında.
        // Tek-tablo aggregate'ler (N+1 yok). Tablolar farklı olduğundan ayrı sorgular —
        // her biri tek round-trip, sunucuda COUNT.
        await using var dbContext = await contextFactory.CreateDbContextAsync(ct);

        // 1) Bir markaya bağlı (BrandId != null) toplam ürün — IX(BrandId, IsDeleted) kapsar.
        var totalProductCount = await dbContext.MainProducts
            .CountAsync(p => p.BrandId != null, ct);

        // 2) En az bir pazaryeri eşleşmesi olan DISTINCT marka — IX(ApplicationBrandId).
        var matchedBrandCount = await dbContext.BrandMarketPlaceMatches
            .Select(m => m.ApplicationBrandId)
            .Distinct()
            .CountAsync(ct);

        // 3) Hiç (görünür) ürünü olmayan marka — NOT EXISTS, MainProducts.BrandId indexli.
        //    Products navigation'ı query filter altında → soft-deleted ürün "yok" sayılır.
        var brandsWithoutProductCount = await dbContext.Brands
            .CountAsync(b => !b.Products.Any(), ct);

        return new BrandKpiDto(totalProductCount, matchedBrandCount, brandsWithoutProductCount);
    }

    private static async Task<IResult> CheckIfTheSameNameExits(IntegrationDbContext dbContext, string normalizedName, int? excludeId = null)
    {
        // DB unique kisiti IX_Brands_NormalizedName (UPPER(TRIM(Name))) uzerinden — app kontrolu de
        // NormalizedName uzerinden olmali ki yalnizca buyuk/kucuk harf-bosluk farkli rename crash etmesin.
        var query = dbContext.Brands.Where(x => x.NormalizedName == normalizedName);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);

        if (await query.AnyAsync())
            return new ErrorResult("Bu isimde bir marka zaten mevcut");
        return new SuccessResult();
    }

    private static async Task<IResult> CheckIfSeoSlugExists(IntegrationDbContext dbContext, string seoSlug, int? excludeId = null)
    {
        var query = dbContext.Brands.Where(x => x.SeoSlug == seoSlug);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);

        if (await query.AnyAsync())
            return new ErrorResult("Bu SEO slug zaten kullanılıyor");
        return new SuccessResult();
    }

    // DB index/backfill formulu UPPER(TRIM("Name")) ile birebir ayni — invariant upper, sadece trim.
    private static string NormalizeName(string name) => (name ?? string.Empty).Trim().ToUpperInvariant();
}

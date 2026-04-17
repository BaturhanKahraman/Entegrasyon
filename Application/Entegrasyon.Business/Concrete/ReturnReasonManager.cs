using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Entegrasyon.Business.Concrete;

public sealed class ReturnReasonManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IMemoryCache memoryCache) : IReturnReasonManager
{
    private const string CacheKey = "ReturnReasons_Active";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<IDataResult<List<ReturnReason>>> GetAllAsync(bool activeOnly = true)
    {
        if (activeOnly && memoryCache.TryGetValue(CacheKey, out List<ReturnReason>? cached) && cached is not null)
            return new SuccessDataResult<List<ReturnReason>>(cached);

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var query = dbContext.ReturnReasons.AsNoTracking().OrderBy(r => r.SortOrder).AsQueryable();
        if (activeOnly)
            query = query.Where(r => r.IsActive);

        var reasons = await query.ToListAsync();

        if (activeOnly)
            memoryCache.Set(CacheKey, reasons, CacheDuration);

        return new SuccessDataResult<List<ReturnReason>>(reasons);
    }

    public async Task<IDataResult<ReturnReason>> GetByIdAsync(int id)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var reason = await dbContext.ReturnReasons.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
        return reason is null
            ? new ErrorDataResult<ReturnReason>(null!, "İade sebebi bulunamadı.")
            : new SuccessDataResult<ReturnReason>(reason);
    }

    public async Task<IResult> CreateAsync(string code, string name, int sortOrder = 0)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        if (await dbContext.ReturnReasons.AnyAsync(r => r.Code == code))
            return new ErrorResult($"'{code}' kodu zaten mevcut.");

        dbContext.ReturnReasons.Add(new ReturnReason
        {
            Code = code.ToUpperInvariant(),
            Name = name,
            SortOrder = sortOrder,
            IsActive = true,
            IsSystem = false
        });

        await dbContext.SaveChangesAsync();
        memoryCache.Remove(CacheKey);
        return new SuccessResult("İade sebebi oluşturuldu.");
    }

    public async Task<IResult> UpdateAsync(int id, string name, int sortOrder, bool isActive)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var reason = await dbContext.ReturnReasons.FindAsync(id);
        if (reason is null) return new ErrorResult("İade sebebi bulunamadı.");

        reason.Name = name;
        reason.SortOrder = sortOrder;
        reason.IsActive = isActive;
        await dbContext.SaveChangesAsync();
        memoryCache.Remove(CacheKey);
        return new SuccessResult("İade sebebi güncellendi.");
    }

    public async Task<IResult> DeactivateAsync(int id)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var reason = await dbContext.ReturnReasons.FindAsync(id);
        if (reason is null) return new ErrorResult("İade sebebi bulunamadı.");
        if (reason.IsSystem) return new ErrorResult("Sistem tanımlı iade sebepleri pasifleştirilemez.");

        reason.IsActive = false;
        await dbContext.SaveChangesAsync();
        memoryCache.Remove(CacheKey);
        return new SuccessResult("İade sebebi pasifleştirildi.");
    }
}

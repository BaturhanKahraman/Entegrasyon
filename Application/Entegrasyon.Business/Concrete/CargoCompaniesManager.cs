using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Business.Mappers;
using Entegrasyon.Entity.Dtos.CargoCompany;
using Entegrasyon.Entity.Logs;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.Business.Extensions;
using Entegrasyon.Business.Utilities;
using Entegrasyon.Entity.Results;
using Entegrasyon.Business.Tenants;

namespace Entegrasyon.Business.Concrete;

public class CargoCompaniesManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IFluentValidator validator,
    IApplicationLogManager applicationLogManager,
    CargoCompanyMapper mapper,
    TenantMemoryCache memoryCache) : ICargoCompaniesManager
{
    private const string CacheKey = "CargoCompanies_All";

    public async Task<IDataResult<List<CargoCompany>>> GetCargoCompanies()
    {
        if (memoryCache.TryGetValue<List<CargoCompany>>(CacheKey, out var cached) && cached is not null)
            return new SuccessDataResult<List<CargoCompany>>(cached);

        using var dbContext = contextFactory.CreateDbContext();
        var result = await dbContext.CargoCompanies.AsNoTracking().ToListAsync();
        memoryCache.Set(CacheKey, result, TimeSpan.FromMinutes(30));
        return new SuccessDataResult<List<CargoCompany>>(result);
    }

    public async Task<IDataResult<List<CargoCompany>>> GetCargoCompanies(string cargoCompanySearchParam)
    {
        var allResult = await GetCargoCompanies();
        if (!allResult.Success) return allResult;

        if (string.IsNullOrWhiteSpace(cargoCompanySearchParam))
            return allResult;

        var filtered = allResult.Data!
            .Where(x => x.Name!.Contains(cargoCompanySearchParam, StringComparison.OrdinalIgnoreCase) ||
                        x.Code!.Contains(cargoCompanySearchParam, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return new SuccessDataResult<List<CargoCompany>>(filtered);
    }

    public async Task<IResult> AddCargoCompany(AddCargoCompanyDto cargoCompanyDto)
    {
        using var dbContext = contextFactory.CreateDbContext();
        await applicationLogManager.AddLog("Kargo şirketi ekleme isteği geldi.", LogType.Brand, LogAction.Add, cargoCompanyDto);
        var cargo = mapper.MapToEntity(cargoCompanyDto);
        var result = LogicRunner.Run(await CheckIfTheSameNameExits(cargo.Name));
        if (result != null)
        {
            await applicationLogManager.AddLog($"Kargo şirketi eklenemedi. {result.Message}", LogType.Brand, LogAction.Add, cargoCompanyDto);
            return new ErrorResult(result.Message!);
        }
        await validator.ValidateAndThrowAsync(cargo);
        dbContext.CargoCompanies.Add(cargo);
        await dbContext.SaveChangesAsync();
        InvalidateCache();
        await applicationLogManager.AddLog("Kargo şirketi başarıyla eklendi.", LogType.Brand, LogAction.Add, cargoCompanyDto);
        return new SuccessResult();
    }

    public async Task<IResult> UpdateCargoCompany(CargoCompany cargoCompany)
    {
        using var dbContext = contextFactory.CreateDbContext();
        await applicationLogManager.AddLog("Kargo şirketi güncelleme isteği geldi.", LogType.Brand, LogAction.Update, cargoCompany);
        var result = LogicRunner.Run(await CheckIfTheSameNameExits(cargoCompany.Name));
        if (result != null)
        {
            await applicationLogManager.AddLog($"Kargo şirketi güncellenemedi. {result.Message}", LogType.Brand, LogAction.Add, cargoCompany);
            return new ErrorResult(result.Message!);
        }
        await validator.ValidateAndThrowAsync(cargoCompany);
        dbContext.CargoCompanies.Update(cargoCompany);
        await dbContext.SaveChangesAsync();
        InvalidateCache();
        await applicationLogManager.AddLog("Kargo şirketi başarıyla güncellendi.", LogType.Brand, LogAction.Update, cargoCompany);
        return new SuccessResult();
    }

    public async Task<IResult> DeleteCargoCompany(CargoCompany cargoCompany)
    {
        using var dbContext = contextFactory.CreateDbContext();
        await applicationLogManager.AddLog("Kargo şirketi silme isteği geldi.", LogType.Brand, LogAction.Delete, cargoCompany);
        if (!await dbContext.CargoCompanies.AnyAsync(x => x.Id == cargoCompany.Id))
        {
            await applicationLogManager.AddLog("Kargo şirketi silme başarısız. İlgili id bulunamadı:", LogType.Brand, LogAction.Delete, cargoCompany);
            return new ErrorResult("Böyle bir Kargo şirketi bulunamadı");
        }
        dbContext.CargoCompanies.Remove(cargoCompany);
        await dbContext.SaveChangesAsync();
        InvalidateCache();
        await applicationLogManager.AddLog("Kargo şirketi başarıyla silindi.", LogType.Brand, LogAction.Delete, cargoCompany);
        return new SuccessResult();
    }

    private async Task<IResult> CheckIfTheSameNameExits(string name)
    {
        var all = await GetCargoCompanies();
        if (all.Data.Any(x => x.Name == name))
            return new ErrorResult("Bu isimde bir Kargo şirketi zaten mevcut");
        return new SuccessResult();
    }

    private void InvalidateCache() => memoryCache.Remove(CacheKey);
}

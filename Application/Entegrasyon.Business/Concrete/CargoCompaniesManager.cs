using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.CargoCompany;
using Entegrasyon.Entity.Logs;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Entegrasyon.Business.Extensions;
using Entegrasyon.Business.Utilities;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Concrete;

public class CargoCompaniesManager : ICargoCompaniesManager
{
    private readonly IntegrationDbContext _dbContext;
    private readonly IFluentValidator _validator;
    private readonly IApplicationLogManager _applicationLogManager;
    private readonly IMapper _mapper;

    public CargoCompaniesManager(IntegrationDbContext dbContext, IFluentValidator validator, IApplicationLogManager applicationLogManager, IMapper mapper)
    {
        _dbContext = dbContext;
        _validator = validator;
        _applicationLogManager = applicationLogManager;
        _mapper = mapper;
    }

    public async Task<IDataResult<List<CargoCompany>>> GetCargoCompanies()
    {
        var result = await _dbContext.CargoCompanies.AsQueryable().ToListAsync();

        return new SuccessDataResult<List<CargoCompany>>(result);
    }

    public async Task<IDataResult<List<CargoCompany>>> GetCargoCompanies(string cargoCompanySearchParam)
    {
        var query = _dbContext.CargoCompanies.AsQueryable();
        if (!string.IsNullOrEmpty(cargoCompanySearchParam))
            query = query.Where(x => x.SearchVector.Matches(cargoCompanySearchParam.ToFullTextSearchQuery()));
        var result = await query.ToListAsync();
        return new SuccessDataResult<List<CargoCompany>>(result);
    }

    public async Task<IResult> AddCargoCompany(AddCargoCompanyDto cargoCompanyDto)
    {
        await _applicationLogManager.AddLog("Kargo şirketi ekleme isteği geldi.", LogType.Brand, LogAction.Add, cargoCompanyDto);
        var cargo = _mapper.Map<AddCargoCompanyDto, CargoCompany>(cargoCompanyDto);
        var result = LogicRunner.Run(await CheckIfTheSameNameExits(cargo.Name));
        if (result != null)
        {
            await _applicationLogManager.AddLog($"Kargo şirketi eklenemedi. {result.Message}", LogType.Brand, LogAction.Add, cargoCompanyDto);
            return new ErrorResult(result.Message);
        }
        await _validator.ValidateAndThrowAsync(cargo);
        _dbContext.CargoCompanies.Add(cargo);
        await _dbContext.SaveChangesAsync();
        await _applicationLogManager.AddLog("Kargo şirketi başarıyla eklendi.", LogType.Brand, LogAction.Add, cargoCompanyDto);
        return new SuccessResult();
    }

    public async Task<IResult> UpdateCargoCompany(CargoCompany cargoCompany)
    {
        await _applicationLogManager.AddLog("Kargo şirketi güncelleme isteği geldi.", LogType.Brand, LogAction.Update, cargoCompany);
        var result = LogicRunner.Run(await CheckIfTheSameNameExits(cargoCompany.Name));
        if (result != null)
        {
            await _applicationLogManager.AddLog($"Kargo şirketi güncellenemedi. {result.Message}", LogType.Brand, LogAction.Add, cargoCompany);
            return new ErrorResult(result.Message);
        }
        await _validator.ValidateAndThrowAsync(cargoCompany);
        _dbContext.CargoCompanies.Update(cargoCompany);
        await _dbContext.SaveChangesAsync();
        await _applicationLogManager.AddLog("Kargo şirketi başarıyla güncellendi.", LogType.Brand, LogAction.Update, cargoCompany);
        return new SuccessResult();
    }

    public async Task<IResult> DeleteCargoCompany(CargoCompany cargoCompany)
    {
        await _applicationLogManager.AddLog("Kargo şirketi silme isteği geldi.", LogType.Brand, LogAction.Delete, cargoCompany);
        if (!await _dbContext.CargoCompanies.AnyAsync(x => x.Id == cargoCompany.Id))
        {
            await _applicationLogManager.AddLog("Kargo şirketi silme başarısız. İlgili id bulunamadı:", LogType.Brand, LogAction.Delete, cargoCompany);
            return new ErrorResult("Böyle bir Kargo şirketi bulunamadı");
        }
        _dbContext.CargoCompanies.Remove(cargoCompany);
        await _dbContext.SaveChangesAsync();
        await _applicationLogManager.AddLog("Kargo şirketi başarıyla silindi.", LogType.Brand, LogAction.Delete, cargoCompany);
        return new SuccessResult();
    }

    private async Task<IResult> CheckIfTheSameNameExits(string name)
    {
        if (await _dbContext.CargoCompanies.AnyAsync(x => x.Name == name))
            return new ErrorResult("Bu isimde bir Kargo şirketi zaten mevcut");
        return new SuccessResult();
    }
}

using AutoMapper;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Validation.FluentValidation;
using Entegrasyon.DataAccess.Abstract;
using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.CargoCompany;
using Entegrasyon.Entity.Logs;
using Microsoft.EntityFrameworkCore;
using Shared.Extensions;
using Shared.Logic;
using Shared.Results;

namespace Entegrasyon.Business.Concrete;

public class CargoCompaniesManager
{
    private readonly ICargoCompanyDal _cargoDal;
    private readonly FluentValidator _validator;
    private readonly IApplicationLogManager _applicationLogManager;
    private readonly IMapper _mapper;
    public CargoCompaniesManager(ICargoCompanyDal cargoDal,FluentValidator validator,IApplicationLogManager applicationLogManager,IMapper mapper)
    {
        _cargoDal = cargoDal;
        _validator = validator;
        _applicationLogManager = applicationLogManager;
        _mapper = mapper;
    }

    public async Task<IDataResult<List<CargoCompany>>> GetCargoCompanies()
    {
        var result = await _cargoDal.GetAllAsync();
        return new SuccessDataResult<List<CargoCompany>>(result);
    }

    public async Task<IDataResult<List<CargoCompany>>> GetCargoCompanies(string cargoCompanySearchParam)
    {
        var result = await _cargoDal.Table
            .WhereIf(!string.IsNullOrEmpty(cargoCompanySearchParam),x=>x.SearchVector.Matches(cargoCompanySearchParam.ToFullTextSearchQuery()))
            .ToListAsync();
        return new SuccessDataResult<List<CargoCompany>>(result);
    } 

    public async Task<IResult> AddCargoCompany(AddCargoCompanyDto cargoCompanyDto)
    {
        await _applicationLogManager.AddLog("Kargo şirketi ekleme isteği geldi.",LogType.Brand,LogAction.Add,cargoCompanyDto);
        var brand = _mapper.Map<AddCargoCompanyDto,CargoCompany>(cargoCompanyDto);
        var result = LogicRunner.Run(await CheckIfTheSameNameExits(brand.Name));
        if(result != null)
        {
            await _applicationLogManager.AddLog($"Kargo şirketi eklenemedi. {result.Message}",LogType.Brand,LogAction.Add,cargoCompanyDto);
            return new ErrorResult(result.Message);
        }
        await _validator.ValidateAndThrowAsync(brand);
        await _cargoDal.AddAsync(brand);
        await _applicationLogManager.AddLog("Kargo şirketi başarıyla eklendi.",LogType.Brand,LogAction.Add,cargoCompanyDto);
        return new SuccessResult();
    }
    public async Task<IResult> UpdateCargoCompany(CargoCompany cargoCompany)
    {
        await _applicationLogManager.AddLog("Kargo şirketi güncelleme isteği geldi.",LogType.Brand,LogAction.Update,cargoCompany);
        var result = LogicRunner.Run(await CheckIfTheSameNameExits(cargoCompany.Name));
        if(result != null)
        {
            await _applicationLogManager.AddLog($"Kargo şirketi güncellenemedi. {result.Message}",LogType.Brand,LogAction.Add,cargoCompany);
            return new ErrorResult(result.Message);
        }
        await _validator.ValidateAndThrowAsync(cargoCompany);
        await _cargoDal.UpdateAsync(cargoCompany);
        await _applicationLogManager.AddLog("Kargo şirketi başarıyla güncellendi.",LogType.Brand,LogAction.Update,cargoCompany);
        return new SuccessResult();
    }
    public async Task<IResult> DeleteCargoCompany(CargoCompany cargoCompany)
    {
        await _applicationLogManager.AddLog("Kargo şirketi silme isteği geldi.",LogType.Brand,LogAction.Delete,cargoCompany);
        if(!await _cargoDal.Exists(x => x.Id == cargoCompany.Id))
        {
            await _applicationLogManager.AddLog("Kargo şirketi silme başarısız. İlgili id bulunamadı:",LogType.Brand,LogAction.Delete,cargoCompany);
            return new ErrorResult("Böyle bir Kargo şirketi bulunamadı");
        }
        await _cargoDal.DeleteAsync(cargoCompany);
        await _applicationLogManager.AddLog("Kargo şirketi başarıyla silindi.",LogType.Brand,LogAction.Delete,cargoCompany);
        return new SuccessResult();
    }


    private async Task<IResult> CheckIfTheSameNameExits(string name)
    {
        if(await _cargoDal.Exists(x => x.Name == name))
            return new ErrorResult("Bu isimde bir Kargo şirketi zaten mevcut");
        return new SuccessResult();
    }
}
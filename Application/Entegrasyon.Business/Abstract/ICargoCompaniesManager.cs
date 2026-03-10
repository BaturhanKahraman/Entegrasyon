using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.CargoCompany;
using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface ICargoCompaniesManager
{
    Task<IDataResult<List<CargoCompany>>> GetCargoCompanies();
    Task<IDataResult<List<CargoCompany>>> GetCargoCompanies(string cargoCompanySearchParam);
    Task<IResult> AddCargoCompany(AddCargoCompanyDto cargoCompanyDto);
    Task<IResult> UpdateCargoCompany(CargoCompany cargoCompany);
    Task<IResult> DeleteCargoCompany(CargoCompany cargoCompany);
}

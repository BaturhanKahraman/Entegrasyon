using Entegrasyon.Entity;
using Entegrasyon.Entity.Dtos.CargoCompany;
using Riok.Mapperly.Abstractions;

namespace Entegrasyon.Business.Mappers;

[Mapper]
public partial class CargoCompanyMapper
{
    [MapperIgnoreTarget(nameof(CargoCompany.SearchVector))]
    public partial CargoCompany MapToEntity(AddCargoCompanyDto dto);

    public partial AddCargoCompanyDto MapToDto(CargoCompany entity);
}

using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Dtos.Brand;
using Riok.Mapperly.Abstractions;

namespace Entegrasyon.Business.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class BrandMapper
{
    public partial Brand MapToEntity(AddBrandDto dto);
    public partial AddBrandDto MapToDto(Brand entity);
}

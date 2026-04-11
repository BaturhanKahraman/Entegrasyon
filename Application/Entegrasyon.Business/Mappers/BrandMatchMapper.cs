using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Matches;
using Riok.Mapperly.Abstractions;

namespace Entegrasyon.Business.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class BrandMatchMapper
{
    [MapProperty("ApplicationBrand.Name", nameof(BrandMarketPlaceMatchDto.ApplicationBrandName))]
    public partial BrandMarketPlaceMatchDto MapToDto(BrandMarketPlaceMatch src);

    public partial BrandMarketPlaceMatch MapToEntity(CreateBrandMarketPlaceMatchDto dto);

    public List<BrandMarketPlaceMatchDto> MapToDtoList(List<BrandMarketPlaceMatch> src)
        => src.Select(MapToDto).ToList();
}

using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Dtos.Brand;
using Shared.DTO;
using Shared.Entity;
using Shared.Results;

namespace Entegrasyon.Business.Abstract;

public interface IBrandManager
{
    Task<IDataResult<List<BrandListDetailDto>>> GetBrandListDetails();
    Task<IResult> AddBrand(AddBrandDto brandDto);
    Task<IResult> UpdateBrand(Brand brand);
    Task<IResult> DeleteBrand(int id);
    Task<IDataResult<Pageable<BrandListDetailDto>>> GetBrandDetailPageable(GetCategoryDetailsPageDto dto);
    Task<IDataResult<Brand>> GetBrandById(int id);
    Task<IDataResult<BrandDetailDto>> GetBrandDetail(int id);
}

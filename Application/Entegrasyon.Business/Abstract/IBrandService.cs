using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Dtos.Brand;
using Entegrasyon.Entity.Requests;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity;

namespace Entegrasyon.Business.Abstract;

public interface IBrandService
{
    Task<IDataResult<List<BrandListDetailDto>>> GetBrandListDetails();
    Task<IResult> AddBrand(AddBrandDto brandDto);
    Task<IResult> UpdateBrand(Brand brand);
    Task<IResult> DeleteBrand(int id);
    Task<IDataResult<Pageable<BrandListDetailDto>>> GetBrandDetailPageable(BrandDetailPaginatedRequest request);
    Task<IDataResult<Brand>> GetBrandById(int id);
    Task<IDataResult<BrandDetailDto>> GetBrandDetail(int id);
}

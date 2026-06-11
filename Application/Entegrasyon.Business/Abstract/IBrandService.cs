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

    /// <summary>
    /// Markalar liste sayfası üst KPI kartları için aggregate özet (toplam ürün,
    /// eşleşmiş marka, ürünsüz marka). Read-path, index-backed, N+1 yok.
    /// </summary>
    Task<BrandKpiDto> GetBrandKpisAsync(CancellationToken ct = default);

    // Storefront
    Task<IDataResult<Brand>> GetBrandBySeoSlugAsync(string slug);
}

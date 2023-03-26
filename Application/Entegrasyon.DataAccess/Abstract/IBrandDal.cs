using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Dtos.Brand;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Abstract;

public interface IBrandDal : IEntityRepository<Brand>
{
    Task<BrandListDetailDto> ConvertToBrandDetail(Brand brand);
}
using Entegrasyon.Entity.Brands;
using Entegrasyon.Entity.Matches;
using Shared.EntityFrameworkCore;

namespace Entegrasyon.DataAccess.Abstract;

public interface IBrandMarketPlaceMatchDal: IEntityRepository<BrandMarketPlaceMatch>
{
}
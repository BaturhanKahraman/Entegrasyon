using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.Business.Abstract;

public interface IStorefrontBannerManager
{
    Task<IDataResult<List<StorefrontBanner>>> GetActiveBannersAsync(int tenantId, BannerPosition position);
    Task<IDataResult<List<StorefrontBanner>>> GetAllBannersAsync(int tenantId);
    Task<IDataResult<StorefrontBanner>> CreateAsync(StorefrontBanner banner);
    Task<IResult> UpdateAsync(StorefrontBanner banner);
    Task<IResult> DeleteAsync(int id);
}

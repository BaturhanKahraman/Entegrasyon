using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.Business.Abstract;

public interface IStorefrontSizeGuideManager
{
    Task<IDataResult<StorefrontSizeGuide?>> GetSizeGuideForCategoryAsync(int tenantId, int categoryId);
    Task<IDataResult<List<StorefrontSizeGuide>>> GetAllSizeGuidesAsync(int tenantId);
    Task<IResult> CreateOrUpdateAsync(StorefrontSizeGuide guide);
    Task<IResult> DeleteAsync(int id);
}

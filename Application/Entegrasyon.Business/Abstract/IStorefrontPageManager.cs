using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.Business.Abstract;

public interface IStorefrontPageManager
{
    Task<IDataResult<List<StorefrontPage>>> GetPublishedPagesAsync(int tenantId);
    Task<IDataResult<StorefrontPage>> GetBySlugAsync(int tenantId, string slug);
}

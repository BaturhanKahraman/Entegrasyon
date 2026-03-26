using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.Business.Abstract;

public interface IStorefrontAbandonedCartManager
{
    Task<IResult> ProcessAbandonedCartsAsync(int tenantId);
    Task<IResult> MarkAsConvertedAsync(Guid cartId);
    Task<IDataResult<List<StorefrontAbandonedCartEmail>>> GetAbandonedCartEmailsAsync(int tenantId);
}

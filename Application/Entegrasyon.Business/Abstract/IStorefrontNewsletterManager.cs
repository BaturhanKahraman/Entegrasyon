using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Storefront;

namespace Entegrasyon.Business.Abstract;

public interface IStorefrontNewsletterManager
{
    Task<IResult> SubscribeAsync(int tenantId, string email, string? name);
    Task<IResult> UnsubscribeAsync(int tenantId, string email);
    Task<IDataResult<List<StorefrontNewsletter>>> GetSubscribersAsync(int tenantId);
}

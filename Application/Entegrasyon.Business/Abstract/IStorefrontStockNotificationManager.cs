using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IStorefrontStockNotificationManager
{
    Task<IResult> SubscribeAsync(int tenantId, Guid productVariantId, string email);
    Task<IDataResult<int>> GetSubscriberCountAsync(int tenantId, Guid productVariantId);
}

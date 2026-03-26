using Entegrasyon.Entity.Results;

namespace Entegrasyon.Business.Abstract;

public interface IStorefrontPushManager
{
    Task<IResult> SubscribeAsync(int tenantId, int? customerId, string endpoint, string p256dh, string auth);
    Task<IResult> UnsubscribeAsync(string endpoint);
    Task<IDataResult<int>> GetSubscriberCountAsync(int tenantId);
}

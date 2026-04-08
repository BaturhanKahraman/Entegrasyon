using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Webhooks;

namespace Entegrasyon.Business.Abstract;

public interface IWebhookManager
{
    Task<List<WebhookSubscription>> GetAllAsync(int tenantId);
    Task<IDataResult<WebhookSubscription>> CreateAsync(int tenantId, string url, string? secret, string[] eventTypes);
    Task<IResult> DeleteAsync(int id);
    Task<IResult> ToggleAsync(int id);
    Task<List<WebhookDeliveryLog>> GetDeliveryLogsAsync(int subscriptionId, int count = 20);
    Task DispatchEventAsync(int tenantId, string eventType, object data);
}

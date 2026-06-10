using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Entegrasyon.Business.Abstract;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Entegrasyon.Entity.Logs;
using Entegrasyon.Entity.Results;
using Entegrasyon.Entity.Webhooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.Concrete;

public class WebhookManager(
    IDbContextFactory<IntegrationDbContext> contextFactory,
    IHttpClientFactory httpClientFactory,
    IApplicationLogManager applicationLogManager,
    ILogger<WebhookManager> logger) : IWebhookManager
{
    private const int MaxFailureCount = 5;

    public async Task<List<WebhookSubscription>> GetAllAsync(int tenantId)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        return await dbContext.WebhookSubscriptions
            .AsNoTracking()
            .Where(w => w.TenantId == tenantId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();
    }

    public async Task<IDataResult<WebhookSubscription>> CreateAsync(int tenantId, string url, string? secret, string[] eventTypes)
    {
        if (string.IsNullOrWhiteSpace(url))
            return new ErrorDataResult<WebhookSubscription>(null!, "URL bos olamaz.");
        if (!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return new ErrorDataResult<WebhookSubscription>(null!, "Webhook URL'i HTTPS olmalidir.");
        if (eventTypes.Length == 0)
            return new ErrorDataResult<WebhookSubscription>(null!, "En az bir event tipi secilmelidir.");

        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var subscription = new WebhookSubscription
        {
            TenantId = tenantId,
            Url = url,
            Secret = secret,
            EventTypes = JsonSerializer.Serialize(eventTypes),
            IsActive = true
        };

        dbContext.WebhookSubscriptions.Add(subscription);
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog($"Webhook olusturuldu: {url}", LogType.Settings, LogAction.Add);
        logger.LogInformation("Webhook created: {Id} {Url}", subscription.Id, url);

        return new SuccessDataResult<WebhookSubscription>(subscription, "Webhook olusturuldu.");
    }

    public async Task<IResult> DeleteAsync(int id)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var sub = await dbContext.WebhookSubscriptions.FindAsync(id);
        if (sub is null) return new ErrorResult("Webhook bulunamadı.");

        sub.IsDeleted = true;
        sub.DeletedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();

        await applicationLogManager.AddLog($"Webhook silindi: {sub.Url}", LogType.Settings, LogAction.Delete);
        return new SuccessResult("Webhook silindi.");
    }

    public async Task<IResult> ToggleAsync(int id)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        var sub = await dbContext.WebhookSubscriptions.FindAsync(id);
        if (sub is null) return new ErrorResult("Webhook bulunamadı.");

        sub.IsActive = !sub.IsActive;
        if (sub.IsActive) sub.FailureCount = 0;
        await dbContext.SaveChangesAsync();

        var msg = sub.IsActive ? "aktif edildi" : "pasife alindi";
        await applicationLogManager.AddLog($"Webhook {msg}: {sub.Url}", LogType.Settings, LogAction.Update);
        return new SuccessResult($"Webhook {msg}.");
    }

    public async Task<List<WebhookDeliveryLog>> GetDeliveryLogsAsync(int subscriptionId, int count = 20)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();
        return await dbContext.WebhookDeliveryLogs
            .AsNoTracking()
            .Where(l => l.SubscriptionId == subscriptionId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task DispatchEventAsync(int tenantId, string eventType, object data)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync();

        var subscriptions = await dbContext.WebhookSubscriptions
            .AsNoTracking()
            .Where(w => w.TenantId == tenantId && w.IsActive && w.FailureCount < MaxFailureCount)
            .ToListAsync();

        foreach (var sub in subscriptions)
        {
            var eventTypes = JsonSerializer.Deserialize<string[]>(sub.EventTypes) ?? [];
            if (!eventTypes.Contains(eventType)) continue;

            await DeliverAsync(dbContext, sub, eventType, data);
        }
    }

    private async Task DeliverAsync(IntegrationDbContext dbContext, WebhookSubscription sub, string eventType, object data)
    {
        var payload = JsonSerializer.Serialize(new
        {
            @event = eventType,
            timestamp = DateTimeOffset.UtcNow,
            data
        });

        var log = new WebhookDeliveryLog
        {
            SubscriptionId = sub.Id,
            EventType = eventType,
            Payload = payload
        };

        try
        {
            var client = httpClientFactory.CreateClient("webhook");
            var request = new HttpRequestMessage(HttpMethod.Post, sub.Url)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };

            if (!string.IsNullOrEmpty(sub.Secret))
            {
                var signature = ComputeHmacSignature(payload, sub.Secret);
                request.Headers.Add("X-Webhook-Signature", $"sha256={signature}");
            }

            request.Headers.Add("X-Webhook-Event", eventType);

            using var response = await client.SendAsync(request);
            log.ResponseCode = (int)response.StatusCode;
            log.ResponseBody = await response.Content.ReadAsStringAsync();
            log.Success = response.IsSuccessStatusCode;

            if (log.Success)
            {
                // Başarılıysa failure counter sıfırla
                await dbContext.WebhookSubscriptions
                    .Where(w => w.Id == sub.Id)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(w => w.FailureCount, 0)
                        .SetProperty(w => w.LastTriggeredAt, DateTimeOffset.UtcNow));
            }
            else
            {
                await IncrementFailureCount(dbContext, sub.Id);
            }
        }
        catch (Exception ex)
        {
            log.Success = false;
            log.ResponseBody = ex.Message;
            logger.LogWarning(ex, "Webhook delivery failed for {SubId} {Url}", sub.Id, sub.Url);

            await IncrementFailureCount(dbContext, sub.Id);
        }

        dbContext.WebhookDeliveryLogs.Add(log);
        await dbContext.SaveChangesAsync();
    }

    private static async Task IncrementFailureCount(IntegrationDbContext dbContext, int subscriptionId)
    {
        await dbContext.WebhookSubscriptions
            .Where(w => w.Id == subscriptionId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(w => w.FailureCount, w => w.FailureCount + 1)
                .SetProperty(w => w.LastTriggeredAt, DateTimeOffset.UtcNow));
    }

    private static string ComputeHmacSignature(string payload, string secret)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var data = Encoding.UTF8.GetBytes(payload);
        var hash = HMACSHA256.HashData(key, data);
        return Convert.ToHexStringLower(hash);
    }
}

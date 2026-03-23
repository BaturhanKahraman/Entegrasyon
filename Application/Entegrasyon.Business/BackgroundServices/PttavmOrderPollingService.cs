using System.Collections.Concurrent;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utility.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Her 5 dakikada PttAVM siparis API'sini poll eder,
/// son 24 saatlik siparis penceresi kullanilir.
/// Multi-tenant hazir: ConcurrentDictionary ile tenant basina son poll zamani takip edilir.
/// </summary>
public class PttavmOrderPollingService(
    IServiceScopeFactory scopeFactory,
    ILogger<PttavmOrderPollingService> logger) : BackgroundService
{
    private const int PttavmMarketPlaceId = MarketPlaceConstants.PttavmMarketPlaceId;
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan OrderLookbackWindow = TimeSpan.FromHours(24);

    // Multi-tenant: tenant basina son poll zamani (key = tenantId)
    private readonly ConcurrentDictionary<int, DateTimeOffset> _lastPollTimes = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(25), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollOrdersAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "PttAVM sipariş polling hatası");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task PollOrdersAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IPttavmOrderService>();

        var tenantId = 0; // TODO: multi-tenant'ta tum aktif tenant'lar iterate edilecek
        var lastPoll = _lastPollTimes.GetOrAdd(tenantId, _ => DateTimeOffset.UtcNow - OrderLookbackWindow);
        var endDate = DateTimeOffset.UtcNow;

        var result = await orderService.SearchOrdersAsync(
            lastPoll.UtcDateTime, endDate.UtcDateTime, false, ct);

        if (!result.Success)
        {
            logger.LogWarning("PttAVM sipariş fetch başarısız: {Message}", result.Message);
            return;
        }

        var orders = result.Data ?? [];
        if (orders.Count > 0)
        {
            // TODO: orderManager.ImportPttavmOrdersAsync(orders) eklendiginde buraya ekle
            logger.LogInformation("PttAVM: {Count} sipariş alındı", orders.Count);
        }
        else
        {
            logger.LogDebug("PttAVM: yeni sipariş yok");
        }

        _lastPollTimes[tenantId] = endDate;
    }
}

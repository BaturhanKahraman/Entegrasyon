using System.Collections.Concurrent;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utility.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Her 60 saniyede Hepsiburada sipariş API'sini poll eder,
/// son 2 saatlik sipariş penceresi kullanılır.
/// Multi-tenant hazır: ConcurrentDictionary ile tenant başına son poll zamanı takip edilir.
/// </summary>
public class HepsiburadaOrderPollingService(
    IServiceScopeFactory scopeFactory,
    ILogger<HepsiburadaOrderPollingService> logger) : BackgroundService
{
    private const int HbMarketPlaceId = MarketPlaceConstants.HepsiburadaMarketPlaceId;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan OrderLookbackWindow = TimeSpan.FromHours(2);

    // Multi-tenant: tenant başına son poll zamanı (key = tenantId)
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
                logger.LogError(ex, "Hepsiburada sipariş polling hatası");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task PollOrdersAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IHepsiburadaOrderService>();

        var tenantId = 0; // TODO: multi-tenant'ta tüm aktif tenant'lar iterate edilecek
        var lastPoll = _lastPollTimes.GetOrAdd(tenantId, _ => DateTimeOffset.UtcNow - OrderLookbackWindow);
        var endDate = DateTimeOffset.UtcNow;

        var result = await orderService.GetOrdersAsync(
            beginDate: lastPoll,
            endDate: endDate,
            offset: 0,
            limit: 50);

        if (!result.Success)
        {
            logger.LogWarning("Hepsiburada sipariş fetch başarısız: {Message}", result.Message);
            return;
        }

        var orders = result.Data ?? [];
        if (orders.Count > 0)
        {
            // TODO: orderManager.ImportHepsiburadaOrdersAsync(orders) eklendiğinde buraya ekle
            logger.LogInformation("Hepsiburada: {Count} sipariş alındı", orders.Count);
        }
        else
        {
            logger.LogDebug("Hepsiburada: yeni sipariş yok");
        }

        _lastPollTimes[tenantId] = endDate;
    }
}

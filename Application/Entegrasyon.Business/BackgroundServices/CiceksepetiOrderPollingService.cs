using System.Collections.Concurrent;
using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Concrete.Ciceksepeti;
using Entegrasyon.Business.Utility.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Her 60 saniyede Çiçeksepeti sipariş API'sini poll eder,
/// son 2 saatlik sipariş penceresi kullanılır.
/// Multi-tenant hazır: ConcurrentDictionary ile tenant başına son poll zamanı takip edilir.
/// </summary>
public class CiceksepetiOrderPollingService(
    IServiceScopeFactory scopeFactory,
    ILogger<CiceksepetiOrderPollingService> logger) : BackgroundService
{
    private const int CiceksepetiMarketPlaceId = MarketPlaceConstants.CiceksepetiMarketPlaceId;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan OrderLookbackWindow = TimeSpan.FromHours(2);

    // Multi-tenant: tenant başına son poll zamanı (key = tenantId)
    private readonly ConcurrentDictionary<int, DateTimeOffset> _lastPollTimes = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollOrdersAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Çiçeksepeti sipariş polling hatası");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task PollOrdersAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var orderService = scope.ServiceProvider.GetRequiredService<ICiceksepetiOrderService>();

        var tenantId = 0; // TODO: multi-tenant'ta tüm aktif tenant'lar iterate edilecek
        var lastPoll = _lastPollTimes.GetOrAdd(tenantId, _ => DateTimeOffset.UtcNow - OrderLookbackWindow);
        var endDate = DateTimeOffset.UtcNow;

        var request = new CiceksepetiGetOrdersRequest(
            StartDate: lastPoll.ToString("o"),
            EndDate: endDate.ToString("o"),
            PageSize: 100,
            Page: 1,
            StatusId: null,
            OrderNo: null,
            OrderItemNo: null);

        var result = await orderService.GetOrdersAsync(request, ct);

        if (!result.Success)
        {
            logger.LogWarning("Çiçeksepeti sipariş fetch başarısız: {Message}", result.Message);
            return;
        }

        var orders = result.Data?.SupplierOrderListWithBranch ?? [];
        if (orders.Count > 0)
        {
            // TODO: orderManager.ImportCiceksepetiOrdersAsync(orders) eklendiğinde buraya ekle
            logger.LogInformation("Çiçeksepeti: {Count} sipariş alındı", orders.Count);
        }
        else
        {
            logger.LogDebug("Çiçeksepeti: yeni sipariş yok");
        }

        _lastPollTimes[tenantId] = endDate;
    }
}

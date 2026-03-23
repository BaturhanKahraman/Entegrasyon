using System.Collections.Concurrent;
using Entegrasyon.Business.Abstract;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

public class PazaramaRefundPollingService(
    IServiceScopeFactory scopeFactory,
    ILogger<PazaramaRefundPollingService> logger,
    IConfiguration configuration) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(5);
    private readonly ConcurrentDictionary<int, DateTimeOffset> _lastPollTimes = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (configuration.GetValue<bool>("Pazarama:UseMock"))
        {
            logger.LogInformation("PazaramaRefundPollingService: UseMock=true, polling devre disi");
            return;
        }

        await Task.Delay(TimeSpan.FromSeconds(25), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollRefundsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Pazarama iade/iptal polling hatasi");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task PollRefundsAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var refundService = scope.ServiceProvider.GetRequiredService<IPazaramaRefundService>();

        var marketPlaceId = 5; // TODO: iterate when multi-tenant
        var lastPoll = _lastPollTimes.GetOrAdd(marketPlaceId, _ => DateTimeOffset.UtcNow.AddDays(-1));
        var now = DateTimeOffset.UtcNow;

        // Poll pending refunds (status=1)
        var refundResult = await refundService.GetRefundsAsync(lastPoll, now, refundStatus: 1);
        if (refundResult.Success && refundResult.Data?.RefundList?.Count > 0)
        {
            logger.LogInformation("Pazarama: {Count} yeni iade talebi bulundu", refundResult.Data.RefundList.Count);
        }

        // Poll pending cancellations (status=1)
        var cancelResult = await refundService.GetCancellationsAsync(lastPoll, now, refundStatus: 1);
        if (cancelResult.Success && cancelResult.Data?.RefundList?.Count > 0)
        {
            logger.LogInformation("Pazarama: {Count} yeni iptal talebi bulundu", cancelResult.Data.RefundList.Count);
        }

        _lastPollTimes[marketPlaceId] = now;
    }
}

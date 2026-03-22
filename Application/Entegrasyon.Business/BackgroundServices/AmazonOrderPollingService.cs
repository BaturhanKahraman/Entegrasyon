using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Utility.Constants;
using Entegrasyon.DataAccess.Concrete.EntityFrameworkCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Amazon sipariş periyodik polling servisi.
/// EU marketplace'lerden yeni siparişleri çeker ve sisteme import eder.
/// </summary>
public class AmazonOrderPollingService(
    IServiceScopeFactory scopeFactory,
    ILogger<AmazonOrderPollingService> logger) : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromMinutes(2);
    private DateTimeOffset _lastPollTime = DateTimeOffset.UtcNow.AddHours(-1);

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
                logger.LogError(ex, "Amazon order polling cycle failed");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task PollOrdersAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IAmazonOrderService>();

        // TODO: MarketplaceIds'leri config'den al
        var marketplaceIds = new[] { "A33AVAJ2PDY3EV" };

        var result = await orderService.GetOrdersAsync(
            _lastPollTime, marketplaceIds,
            orderStatuses: new[] { "Unshipped", "PartiallyShipped" },
            ct: ct);

        if (result.Success && result.Data?.Any() == true)
        {
            logger.LogInformation("Amazon: {Count} yeni sipariş bulundu", result.Data.Count);
            // TODO: IOrderManager.ImportAmazonOrdersAsync ile sisteme import et
        }
        else
        {
            logger.LogDebug("Amazon order polling: yeni sipariş yok");
        }

        _lastPollTime = DateTimeOffset.UtcNow;
    }
}

using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Trendyol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Her 2 dakikada Trendyol getShipmentPackages endpoint'ini poll eder,
/// yeni siparişleri import eder.
/// </summary>
public class TrendyolOrderPollingService(
    IServiceScopeFactory scopeFactory,
    ILogger<TrendyolOrderPollingService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(2);
    private DateTimeOffset _lastPollTime = DateTimeOffset.UtcNow.AddDays(-1); // İlk çalışmada son 1 günü çek

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollOrdersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during Trendyol order polling");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task PollOrdersAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var orderService = scope.ServiceProvider.GetRequiredService<ITrendyolOrderService>();
        var orderManager = scope.ServiceProvider.GetRequiredService<IOrderManager>();

        var query = new TrendyolOrderQueryParams(
            StartDate: _lastPollTime,
            EndDate: DateTimeOffset.UtcNow);

        var result = await orderService.FetchOrdersAsync(query);

        if (!result.Success)
        {
            logger.LogWarning("Trendyol order fetch failed: {Message}", result.Message);
            return;
        }

        if (result.Data.Count > 0)
        {
            var importResult = await orderManager.ImportTrendyolOrdersAsync(result.Data);
            logger.LogInformation("Order poll completed. Fetched={Fetched}, Result={Message}",
                result.Data.Count, importResult.Message);
        }

        _lastPollTime = DateTimeOffset.UtcNow;
    }
}

using System.Collections.Concurrent;
using Entegrasyon.Business.Abstract;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Her 2 dakikada Pazarama sipariş API'sini poll eder,
/// yeni siparişleri import eder.
/// Pazarama:UseMock = true ise polling yapılmaz.
/// </summary>
public class PazaramaOrderPollingService(
    IServiceScopeFactory scopeFactory,
    ILogger<PazaramaOrderPollingService> logger,
    IConfiguration configuration) : BackgroundService
{
    private const int PazaramaMarketPlaceId = 5;
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(2);
    private readonly ConcurrentDictionary<int, DateTimeOffset> _lastPollTimes = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Mock modda polling yapma
        if (configuration.GetValue<bool>("Pazarama:UseMock"))
        {
            logger.LogInformation("PazaramaOrderPollingService: UseMock=true, polling devre dışı");
            return;
        }

        await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollOrdersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Pazarama siparis polling hatasi");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task PollOrdersAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IPazaramaOrderService>();
        var orderManager = scope.ServiceProvider.GetRequiredService<IOrderManager>();

        var lastPoll = _lastPollTimes.GetOrAdd(PazaramaMarketPlaceId, _ => DateTimeOffset.UtcNow.AddDays(-1));
        var now = DateTimeOffset.UtcNow;

        var result = await orderService.FetchOrdersAsync(lastPoll, now);

        if (!result.Success)
        {
            logger.LogWarning("Pazarama siparis fetch basarisiz: {Message}", result.Message);
            return;
        }

        if (result.Data.Count > 0)
        {
            await orderManager.ImportPazaramaOrdersAsync(result.Data);
            logger.LogInformation("Pazarama: {Count} siparis import edildi", result.Data.Count);
        }

        _lastPollTimes[PazaramaMarketPlaceId] = now;
    }
}

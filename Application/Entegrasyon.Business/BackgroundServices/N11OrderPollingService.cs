using Entegrasyon.Business.Abstract;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Her 2 dakikada N11 sipariş API'sini poll eder,
/// yeni siparişleri import eder.
/// N11:UseMock = true ise polling yapılmaz.
/// </summary>
public class N11OrderPollingService(
    IServiceScopeFactory scopeFactory,
    ILogger<N11OrderPollingService> logger,
    IConfiguration configuration) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(2);
    private DateTimeOffset _lastPollTime = DateTimeOffset.UtcNow.AddDays(-1); // İlk çalışmada son 1 günü çek

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Mock modda polling yapma
        if (configuration.GetValue<bool>("N11:UseMock"))
        {
            logger.LogInformation("N11OrderPollingService: UseMock=true, polling devre dışı");
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
                logger.LogError(ex, "N11 siparis polling hatasi");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task PollOrdersAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IN11OrderService>();
        var orderManager = scope.ServiceProvider.GetRequiredService<IOrderManager>();

        var result = await orderService.FetchOrdersAsync(startDate: _lastPollTime);

        if (!result.Success)
        {
            logger.LogWarning("N11 siparis fetch basarisiz: {Message}", result.Message);
            return;
        }

        if (result.Data.Count > 0)
        {
            await orderManager.ImportN11OrdersAsync(result.Data);
            logger.LogInformation("N11: {Count} siparis import edildi", result.Data.Count);
        }

        _lastPollTime = DateTimeOffset.UtcNow;
    }
}

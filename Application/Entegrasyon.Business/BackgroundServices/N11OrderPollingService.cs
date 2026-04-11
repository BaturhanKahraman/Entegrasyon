using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Tenants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Her 2 dakikada N11 sipariş API'sini poll eder,
/// yeni siparişleri import eder. WireMock'a yönlenmiş dev ortamda da
/// çalışır — IN11OrderService WireMock stub'larından response alır.
/// </summary>
public class N11OrderPollingService(
    IServiceScopeFactory scopeFactory,
    ITenantRegistry tenantRegistry,
    ILogger<N11OrderPollingService> logger)
    : TenantAwarePollingService(scopeFactory, tenantRegistry, logger)
{
    protected override TimeSpan PollInterval => TimeSpan.FromMinutes(2);
    protected override string? RequiredFeature => "Permissions.Integrations.View";

    protected override async Task PollForTenantAsync(
        IServiceProvider services, int tenantId,
        DateTimeOffset lastPoll, CancellationToken ct)
    {
        var orderService = services.GetRequiredService<IN11OrderService>();
        var orderManager = services.GetRequiredService<IOrderManager>();

        var result = await orderService.FetchOrdersAsync(startDate: lastPoll);

        if (!result.Success)
        {
            logger.LogWarning("N11 Sipariş fetch basarisiz for tenant {TenantId}: {Message}",
                tenantId, result.Message);
            return;
        }

        if (result.Data.Count > 0)
        {
            await orderManager.ImportN11OrdersAsync(result.Data);
            logger.LogInformation("Tenant {TenantId}: N11: {Count} Sipariş import edildi",
                tenantId, result.Data.Count);
        }
    }
}

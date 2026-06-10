using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Tenants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Her 2 dakikada Pazarama sipariş API'sini poll eder,
/// yeni siparişleri import eder. WireMock'a yönlenmiş dev ortamda da
/// çalışır — IPazaramaOrderService WireMock stub'larından response alır.
/// </summary>
public class PazaramaOrderPollingService(
    IServiceScopeFactory scopeFactory,
    ITenantRegistry tenantRegistry,
    ILogger<PazaramaOrderPollingService> logger)
    : TenantAwarePollingService(scopeFactory, tenantRegistry, logger)
{
    protected override TimeSpan PollInterval => TimeSpan.FromMinutes(2);
    protected override string? RequiredFeature => "Permissions.Integrations.View";

    protected override async Task PollForTenantAsync(
        IServiceProvider services, int tenantId,
        DateTimeOffset lastPoll, CancellationToken ct)
    {
        var orderService = services.GetRequiredService<IPazaramaOrderService>();
        var orderManager = services.GetRequiredService<IOrderManager>();

        var now = DateTimeOffset.UtcNow;

        var result = await orderService.FetchOrdersAsync(lastPoll, now);

        if (!result.Success)
        {
            logger.LogWarning("Pazarama Sipariş fetch başarısız for tenant {TenantId}: {Message}",
                tenantId, result.Message);
            return;
        }

        if (result.Data.Count > 0)
        {
            await orderManager.ImportPazaramaOrdersAsync(result.Data);
            logger.LogInformation("Tenant {TenantId}: Pazarama: {Count} Sipariş import edildi",
                tenantId, result.Data.Count);
        }
    }
}

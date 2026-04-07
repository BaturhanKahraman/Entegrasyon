using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Tenants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Her 2 dakikada Pazarama sipariş API'sini poll eder,
/// yeni siparişleri import eder.
/// Pazarama:UseMock = true ise polling yapılmaz.
/// </summary>
public class PazaramaOrderPollingService(
    IServiceScopeFactory scopeFactory,
    ITenantRegistry tenantRegistry,
    ILogger<PazaramaOrderPollingService> logger,
    IConfiguration configuration)
    : TenantAwarePollingService(scopeFactory, tenantRegistry, logger)
{
    protected override TimeSpan PollInterval => TimeSpan.FromMinutes(2);
    protected override string? RequiredFeature => "Permissions.Integrations.View";

    protected override async Task PollForTenantAsync(
        IServiceProvider services, int tenantId,
        DateTimeOffset lastPoll, CancellationToken ct)
    {
        if (configuration.GetValue<bool>("Pazarama:UseMock"))
        {
            logger.LogInformation("PazaramaOrderPollingService: UseMock=true, polling devre dışı for tenant {TenantId}",
                tenantId);
            return;
        }

        var orderService = services.GetRequiredService<IPazaramaOrderService>();
        var orderManager = services.GetRequiredService<IOrderManager>();

        var now = DateTimeOffset.UtcNow;

        var result = await orderService.FetchOrdersAsync(lastPoll, now);

        if (!result.Success)
        {
            logger.LogWarning("Pazarama Sipariş fetch basarisiz for tenant {TenantId}: {Message}",
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

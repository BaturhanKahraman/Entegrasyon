using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Tenants;
using Entegrasyon.Entity.Dtos.Trendyol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Her 2 dakikada Trendyol getShipmentPackages endpoint'ini poll eder,
/// yeni siparisleri import eder. Tum aktif tenant'lar icin calisir.
/// </summary>
public class TrendyolOrderPollingService(
    IServiceScopeFactory scopeFactory,
    ITenantRegistry tenantRegistry,
    ILogger<TrendyolOrderPollingService> logger)
    : TenantAwarePollingService(scopeFactory, tenantRegistry, logger)
{
    protected override TimeSpan PollInterval => TimeSpan.FromMinutes(2);
    protected override string? RequiredFeature => "Permissions.Integrations.View";

    protected override async Task PollForTenantAsync(
        IServiceProvider services, int tenantId,
        DateTimeOffset lastPoll, CancellationToken ct)
    {
        var orderService = services.GetRequiredService<ITrendyolOrderService>();
        var orderManager = services.GetRequiredService<IOrderManager>();

        var query = new TrendyolOrderQueryParams(
            StartDate: lastPoll,
            EndDate: DateTimeOffset.UtcNow);

        var result = await orderService.FetchOrdersAsync(query);

        if (!result.Success)
        {
            logger.LogWarning("Trendyol order fetch failed for tenant {TenantId}: {Message}",
                tenantId, result.Message);
            return;
        }

        if (result.Data.Count > 0)
        {
            var importResult = await orderManager.ImportTrendyolOrdersAsync(result.Data);
            logger.LogInformation(
                "Tenant {TenantId}: Order poll completed. Fetched={Fetched}, Result={Message}",
                tenantId, result.Data.Count, importResult.Message);
        }
    }
}

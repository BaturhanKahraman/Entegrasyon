using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Tenants;
using Entegrasyon.Business.Utility.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Amazon sipariş periyodik polling servisi.
/// EU marketplace'lerden yeni siparişleri çeker ve sisteme import eder.
/// Tum aktif tenant'lar icin calisir.
/// </summary>
public class AmazonOrderPollingService(
    IServiceScopeFactory scopeFactory,
    ITenantRegistry tenantRegistry,
    ILogger<AmazonOrderPollingService> logger)
    : TenantAwarePollingService(scopeFactory, tenantRegistry, logger)
{
    protected override TimeSpan PollInterval => TimeSpan.FromMinutes(2);
    protected override string? RequiredFeature => "Permissions.Integrations.View";

    protected override async Task PollForTenantAsync(
        IServiceProvider services, int tenantId,
        DateTimeOffset lastPoll, CancellationToken ct)
    {
        var orderService = services.GetRequiredService<IAmazonOrderService>();

        // TODO: MarketplaceIds'leri config'den al
        var marketplaceIds = new[] { "A33AVAJ2PDY3EV" };

        var result = await orderService.GetOrdersAsync(
            lastPoll, marketplaceIds,
            orderStatuses: new[] { "Unshipped", "PartiallyShipped" },
            ct: ct);

        if (result.Success && result.Data?.Any() == true)
        {
            logger.LogInformation("Tenant {TenantId}: Amazon: {Count} yeni sipariş bulundu",
                tenantId, result.Data.Count);
            // TODO: IOrderManager.ImportAmazonOrdersAsync ile sisteme import et
        }
        else
        {
            logger.LogDebug("Tenant {TenantId}: Amazon order polling: yeni sipariş yok", tenantId);
        }
    }
}

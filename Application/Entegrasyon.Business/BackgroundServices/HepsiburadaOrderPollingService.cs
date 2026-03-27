using Entegrasyon.Business.Abstract;
using Entegrasyon.Business.Tenants;
using Entegrasyon.Business.Utility.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Entegrasyon.Business.BackgroundServices;

/// <summary>
/// Her 60 saniyede Hepsiburada sipariş API'sini poll eder,
/// son 2 saatlik sipariş penceresi kullanılır.
/// Tum aktif tenant'lar icin calisir.
/// </summary>
public class HepsiburadaOrderPollingService(
    IServiceScopeFactory scopeFactory,
    ITenantRegistry tenantRegistry,
    ILogger<HepsiburadaOrderPollingService> logger)
    : TenantAwarePollingService(scopeFactory, tenantRegistry, logger)
{
    private const int HbMarketPlaceId = MarketPlaceConstants.HepsiburadaMarketPlaceId;
    private static readonly TimeSpan OrderLookbackWindow = TimeSpan.FromHours(2);

    protected override TimeSpan PollInterval => TimeSpan.FromSeconds(60);
    protected override string? RequiredFeature => "Permissions.Integrations.View";

    protected override async Task PollForTenantAsync(
        IServiceProvider services, int tenantId,
        DateTimeOffset lastPoll, CancellationToken ct)
    {
        var orderService = services.GetRequiredService<IHepsiburadaOrderService>();

        var effectiveLastPoll = lastPoll < DateTimeOffset.UtcNow - OrderLookbackWindow
            ? DateTimeOffset.UtcNow - OrderLookbackWindow
            : lastPoll;
        var endDate = DateTimeOffset.UtcNow;

        var result = await orderService.GetOrdersAsync(
            beginDate: effectiveLastPoll,
            endDate: endDate,
            offset: 0,
            limit: 50);

        if (!result.Success)
        {
            logger.LogWarning("Hepsiburada sipariş fetch başarısız for tenant {TenantId}: {Message}",
                tenantId, result.Message);
            return;
        }

        var orders = result.Data ?? [];
        if (orders.Count > 0)
        {
            // TODO: orderManager.ImportHepsiburadaOrdersAsync(orders) eklendiğinde buraya ekle
            logger.LogInformation("Tenant {TenantId}: Hepsiburada: {Count} sipariş alındı",
                tenantId, orders.Count);
        }
        else
        {
            logger.LogDebug("Tenant {TenantId}: Hepsiburada: yeni sipariş yok", tenantId);
        }
    }
}
